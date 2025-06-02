using Microsoft.Extensions.Configuration;

namespace ddns_dotnet.Extensions;

public static class IConfigurationExtensions
{
    public static AppSettings? GetAppSettings(this IConfiguration configuration)
    {
        return configuration.GetRequiredSection(nameof(AppSettings)).Get<AppSettings>();
    }
}