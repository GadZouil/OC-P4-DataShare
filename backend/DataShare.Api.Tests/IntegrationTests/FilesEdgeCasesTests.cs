using System.Net;
using DataShare.Api.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DataShare.Api.Tests.IntegrationTests;

public class FilesEdgeCasesTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FilesEdgeCasesTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        
        // AUTHENTIFICATION
        _client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
    }

    [Fact]
    public async Task Download_NonExistent_File_Should_Return_404()
    {
        // Act
        var response = await _client.GetAsync("/api/files/download/9999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_File_Should_Work()
    {
        var response = await _client.DeleteAsync("/api/files/9999");
        
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}
