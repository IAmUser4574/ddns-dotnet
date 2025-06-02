using ddns_dotnet.Services;

namespace ddns_dotnet.Tests.Services;

public class IpLookupTests
{
    [Fact]
    public async Task IpLookup_ReturnsNotNull()
    {
        IpLookup sut = new IpLookup(
            new HttpClient(), 
            TestDependencies.GetConfiguration(), 
            TestDependencies.GetLogger<IpLookup>());

        string? ip = await sut.GetAsync();
        
        Assert.NotNull(ip);
    }
}