using Microsoft.Extensions.Logging;
using ddns_dotnet.Services;

namespace ddns_dotnet;

public class Application(
    Ddns ddns,
    ILogger<Application> logger)
{
    public async Task Run(string[] args)
    {
        logger.LogInformation("Application Running...");
        
        CancellationToken ctx = CancellationToken.None;
        await ddns.StartAsync(ctx);
        
        // prevent program exit
        await Task.Delay(-1, ctx);
    }
}