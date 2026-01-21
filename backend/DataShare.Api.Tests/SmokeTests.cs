using System.Net;
using System.Threading.Tasks;
using DataShare.Api.Tests.Infrastructure;
using Xunit;

namespace DataShare.Api.Tests;

public class SmokeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SmokeTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_returns_200()
    {
        var client = _factory.CreateClient();

        // adapte si ton endpoint health est différent
        var res = await client.GetAsync("/health");

        Assert.True(res.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);
    }
}
