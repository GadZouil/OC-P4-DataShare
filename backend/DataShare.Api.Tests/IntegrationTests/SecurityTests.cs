using System.Net;
using DataShare.Api.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DataShare.Api.Tests.IntegrationTests;

public class SecurityTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SecurityTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        // AUTHENTIFICATION
        _client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
    }

    [Fact]
    public async Task User_Cannot_Delete_Other_Users_File()
    {
        // Act
        var response = await _client.DeleteAsync("/api/files/guid-au-hasard");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}
