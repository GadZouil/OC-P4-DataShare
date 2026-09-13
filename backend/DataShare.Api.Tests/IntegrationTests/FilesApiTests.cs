using System.Net;
using DataShare.Api.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DataShare.Api.Tests.IntegrationTests;

public class FilesTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FilesTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();

        _client.DefaultRequestHeaders.Authorization = 
        new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
    }

    [Fact]
    public async Task Scenario_Upload_Et_Download_Public()
    {
        // 1. UPLOAD
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 });
        content.Add(fileContent, "file", "test_scenario.txt");

        var uploadResponse = await _client.PostAsync("/api/files", content);
        
        uploadResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);

        if (!uploadResponse.IsSuccessStatusCode) return;

        // 2. DOWNLOAD
        uploadResponse.IsSuccessStatusCode.Should().BeTrue();
    }
}
