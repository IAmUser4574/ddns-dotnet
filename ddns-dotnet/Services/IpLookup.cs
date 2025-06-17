using System.Text.RegularExpressions;
using ddns_dotnet.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ddns_dotnet.Services;

public class IpLookup(
    HttpClient httpClient,
    IConfiguration config,
    ILogger<IpLookup> logger)
{
    private readonly Regex _ipv4Regex = new Regex(@"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$");

    /// <summary>
    /// Get current IPV4 via third party services
    /// </summary>
    /// <returns></returns>
    public async Task<string?> GetAsync()
    {
        // get IP API sources from config
        IReadOnlyList<string> sources = config.GetAppSettings()?.Ipv4ApiSources ?? new List<string>();
        if (sources.Count == 0)
        {
            logger.LogWarning("No IP API sources configured.");
            return null;
        }
        if (sources.Count == 1)
        {
            logger.LogWarning("Multiple IP API sources required.");
            return null;

        }

        try
        {
            List<string> ipResults = new();
            IEnumerable<Task<string?>> tasks = sources.Select(async source =>
            {
                try
                {
                    string ip = await httpClient.GetStringAsync(new Uri(source));
                    logger.LogInformation("Fetched IP from {source}: {ip}", source, ip);
                    return ip;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to fetch IP from {source}", source);
                    return null;
                }
            });

            string?[] results = await Task.WhenAll(tasks);

            // filter valid IPv4 addresses
            foreach (string? ip in results)
            {
                if (string.IsNullOrWhiteSpace(ip))
                {
                    logger.LogInformation("Ignoring invalid IP: {ip}", ip);
                    continue;
                }

                if (_ipv4Regex.IsMatch(ip))
                {
                    ipResults.Add(ip);
                }
            }

            if (ipResults.Count == 0)
            {
                logger.LogWarning("No valid IPs retrieved from sources.");
                return null;
            }

            // determine most frequent valid IP
            IGrouping<string, string> mostCommonIp = ipResults
                .GroupBy(ip => ip)
                .OrderByDescending(g => g.Count())
                .First();

            if (mostCommonIp.Count() >= ipResults.Count - 1)
            {
                return mostCommonIp.Key;
            }

            logger.LogWarning("IP mismatch: not enough agreement among sources.");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to GetAsync.\n{ex}", ex);
            return null;
        }
    }
}