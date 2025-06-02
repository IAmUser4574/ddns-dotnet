using Microsoft.Extensions.Configuration;
using ddns_dotnet.Extensions;
using Microsoft.Extensions.Logging;

namespace ddns_dotnet.Tests;

public class TestDependencies
{
    private static readonly string TestEmail = "YOUR_EMAIL_HERE";
    private static readonly string TestKey = "YOUR_KEY_HERE";
    
    [Fact]
    public void Configuration_Works()
    {
        IConfiguration config = GetConfiguration();
        
        Assert.Equal(TestEmail, config.GetAppSettings()?.CloudflareEmail); 
        Assert.Equal("https://api.ipify.org", config.GetAppSettings()?.Ipv4ApiSources[0]);
    }

    public static ILogger<T> GetLogger<T>() => new LoggerFactory().CreateLogger<T>();
    public static IConfiguration GetConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string> {
            {"AppSettings:CloudflareEmail", TestEmail},
            {"AppSettings:CloudflareApiKey", TestKey},
            {"AppSettings:UpdateInterval", "00:00:30.00"},
            {"AppSettings:Ipv4ApiSources:0", "https://api.ipify.org"},
            {"AppSettings:Ipv4ApiSources:1", "https://icanhazip.com"},
            {"AppSettings:Ipv4ApiSources:2", "https://ipecho.net/plain"},
            {"AppSettings:DryRun", "true"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();
        
        return configuration;
    }
}