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
        string? currentIp = await ipLookup.GetAsync();
        if (currentIp is null or "")
        {
            logger.LogError("No current ip found.");
            return;
        }

        var email = configuration.GetAppSettings()?.CloudflareEmail;
        var apiKey = configuration.GetAppSettings()?.CloudflareApiKey;
        if (!ConfigurationValidated(email, apiKey))
        {
            return;
        }
        
        using CloudFlareClient cloudFlareClient = new CloudFlareClient(
            configuration.GetAppSettings()?.CloudflareEmail, 
            configuration.GetAppSettings()?.CloudflareApiKey);
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

                if (aRecord.Content == currentIp) continue;
                
                logger.LogInformation("A record needs updating.");
                // create update A record
                ModifiedDnsRecord recordToUpdate = new ModifiedDnsRecord()
                {
                    Type = DnsRecordType.A,
                    Content = aRecord.Content,
                    Name = aRecord.Name,
                };

                if (configuration.GetAppSettings()!.DryRun)
                {
                    logger.LogInformation("Dry run, not updating any records.");
                    continue;
                }
                CloudFlareResult<DnsRecord>? updateRes = await cloudFlareClient.Zones.DnsRecords.UpdateAsync(zone.Id, aRecord.Id, recordToUpdate);
                
                if (updateRes.Success)
                {
                    logger.LogInformation($"Updated record {aRecord.Id} successfully.");
                }
                else
                {
                    logger.LogError($"Failed to update zone record {aRecord.Name}");
                }
            }
        }
    }

    private bool ConfigurationValidated(string? email, string? apiKey)
    {
        if (email is null or "" || !MailAddress.TryCreate(email, out var mailAddress))
        {
            logger.LogError("Invalid email address");
            return false;
        }

        if (apiKey is null or "")
        {
            logger.LogError("Invalid API Key");
            return false;
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