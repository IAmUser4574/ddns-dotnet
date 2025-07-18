using System.Net.Mail;
using CloudFlare.Client;
using CloudFlare.Client.Api.Result;
using CloudFlare.Client.Api.Zones;
using CloudFlare.Client.Api.Zones.DnsRecord;
using CloudFlare.Client.Enumerators;
using ddns_dotnet.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ddns_dotnet.Services;

public class Ddns(IpLookup ipLookup,
    ILogger<Application> logger,
    IConfiguration configuration) : IHostedService
{
    private async Task RunDdns()
    {
        string? targetIp = await ipLookup.GetAsync();
        if (targetIp is null or "")
        {
            logger.LogError("No current ip found");
            return;
        }

        if (!ConfigurationValidated(out CloudflareCredentials cloudflareCredentials))
        {
            return;
        }

        try
        {
            await RunCloudflare(cloudflareCredentials, targetIp);
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to RunCloudflare.\n{ex}");
        }
    }

    /// <summary>
    /// Get all Cloudflare zones and updated all out-of-date A records present with targetIp
    /// </summary>
    /// <param name="email"></param>
    /// <param name="apiKey"></param>
    /// <param name="targetIp"></param>
    private async Task RunCloudflare(CloudflareCredentials credentials, string targetIp)
    {
        using CloudFlareClient cloudFlareClient = credentials.Email is not null
        ? new CloudFlareClient(credentials.Email, credentials.ApiKey)
        : new CloudFlareClient(credentials.ApiToken);

        // get cf api data, iterate over it, set A records if there isn't a match, confirm results
        CloudFlareResult<IReadOnlyList<Zone>>? zoneResults = await cloudFlareClient.Zones.GetAsync();

        if (zoneResults is null ||
            !zoneResults.Success ||
            zoneResults.Result.Count == 0)
        {
            logger.LogError("Failed to find any zones in the Cloudflare API...");
            return;
        }

        foreach (Zone zone in zoneResults.Result)
        {
            logger.LogInformation($"Evaluating zone: {zone.Name}");
            CloudFlareResult<IReadOnlyList<DnsRecord>>? dnsRecords = await cloudFlareClient.Zones.DnsRecords.GetAsync(zone.Id);
            foreach (DnsRecord aRecord in dnsRecords.Result.Where(dnsRecord => dnsRecord.Type == DnsRecordType.A))
            {
                logger.LogInformation($"Evaluating A record '{aRecord.Name}', id: {aRecord.Id}, content: {aRecord.Content}");

                if (aRecord.Content == targetIp) continue;

                logger.LogInformation($"A record needs updating from {aRecord.Content} to {targetIp}");
                // create update A record
                ModifiedDnsRecord recordToUpdate = new ModifiedDnsRecord()
                {
                    // set the new IP
                    Content = targetIp,
                    // leave the remaining fields undisturbed
                    Type = aRecord.Type,
                    Name = aRecord.Name,
                    Comment = aRecord.Comment,
                    Tags = aRecord.Tags,
                    Ttl = aRecord.Ttl,
                    Proxied = aRecord.Proxied,
                    Priority = aRecord.Priority,
                };

                if (configuration.GetAppSettings()!.DryRun)
                {
                    logger.LogInformation("Dry run, not updating any records");
                    continue;
                }
                CloudFlareResult<DnsRecord>? updateRes = await cloudFlareClient.Zones.DnsRecords.UpdateAsync(zone.Id, aRecord.Id, recordToUpdate);

                if (updateRes.Success)
                {
                    logger.LogInformation($"Updated record {aRecord.Id} successfully");
                }
                else
                {
                    logger.LogError($"Failed to update zone record {aRecord.Name}");
                }
            }
        }
    }

    private bool ConfigurationValidated(out CloudflareCredentials cloudflareCredentials)
    {
        cloudflareCredentials = new CloudflareCredentials(
            configuration.GetAppSettings()?.CloudflareEmail,
            configuration.GetAppSettings()?.CloudflareApiKey,
            configuration.GetAppSettings()?.CloudflareApiToken);

        // email/key
        if (cloudflareCredentials.ApiToken is null or "")
        {
            if (cloudflareCredentials.Email is null or "" || !MailAddress.TryCreate(cloudflareCredentials.Email, out _))
            {
                logger.LogError("Invalid email address");
                return false;
            }

            if (cloudflareCredentials.ApiKey is null or "")
            {
                logger.LogError("Invalid API Key");
                return false;
            }
        }
        // token
        else
        {
            if (cloudflareCredentials.ApiToken is null or "")
            {
                logger.LogError("Invalid API Token");
                return false;
            }
        }

        return true;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested == false)
        {
            await RunDdns();
            await Task.Delay(configuration.GetAppSettings()?.UpdateInterval ?? TimeSpan.FromMinutes(5), cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}