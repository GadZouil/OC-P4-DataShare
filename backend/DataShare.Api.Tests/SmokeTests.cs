using System.Net;
using DataShare.Api.Tests.Infrastructure;
using Xunit;

namespace DataShare.Api.Tests;

public class SmokeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SmokeTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Api_starts_and_returns_a_response()
    {
        var resp = await _client.GetAsync("/");
        Assert.NotEqual(HttpStatusCode.InternalServerError, resp.StatusCode);
    }
}
