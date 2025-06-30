using System;
using System.IO;
using System.Threading.Tasks;
using ddns_dotnet.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ddns_dotnet;

public abstract class Program
{
    public static async Task Main(string[] args)
    {
        var builder = new ConfigurationBuilder();
        var appConfiguration = BuildConfig(builder);

        // https://github.com/serilog/serilog/wiki/Configuration-Basics
        // https://benfoster.io/blog/serilog-best-practices/
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(appConfiguration)
            .CreateLogger();

        Log.Logger.Information("Application Starting");

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IConfiguration>(provider => appConfiguration);
                services.AddSingleton<Application>();
                services.AddSingleton<Ddns>();
                services.AddSingleton<IpLookup>();
                services.AddSingleton<HttpClient>();
            })
            .UseSerilog()
            .Build();

        _services = host.Services.CreateScope().ServiceProvider;

        await _services.GetRequiredService<Application>().Run(args);
    }

    private static IServiceProvider? _services;

    public static TService? GetService<TService>()
    {
        return _services != null ? _services.GetService<TService>() : default;
    }

    static IConfigurationRoot BuildConfig(IConfigurationBuilder builder)
    {
        builder.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(@"appsettings.json", optional: false, reloadOnChange: true)  // .json file must be set to "Content" & "Copy Always" or "Copy if newer"
            .AddEnvironmentVariables();

        return builder.Build();
    }
}

public class AppSettings
{
    public string? CloudflareEmail { get; init; }
    public string? CloudflareApiKey { get; init; }
    public string? CloudflareApiToken { get; init; }
    public required TimeSpan UpdateInterval { get; init; }
    public required IReadOnlyList<string> Ipv4ApiSources { get; init; }
    public bool DryRun { get; init; }
}