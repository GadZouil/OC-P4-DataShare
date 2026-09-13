using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DataShare.Api.Data;
using DataShare.Api.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Text.Json;

namespace DataShare.Api.Tests.IntegrationTests;

public class PublicFileTests : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public PublicFileTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Anonymous", "true");
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataShareDbContext>();
        db.Files.RemoveRange(db.Files);
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Anonymous_Upload_Should_Return_Token_And_File_Details()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 10, 20, 30, 40 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "anon.jpg");

        // Act
        var response = await _client.PostAsync("/api/public/files", content);

        // Assert
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        string token = json.GetProperty("token").GetString()!;
        
        token.Should().NotBeNullOrEmpty();
        
        string fileName = json.GetProperty("originalFileName").GetString()!;
        fileName.Should().Be("anon.jpg");
    }

    [Fact]
    public async Task Download_With_Valid_Token_Should_Return_File()
    {
        // 1. Upload
        var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 99, 88, 77 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        uploadContent.Add(fileContent, "file", "download_me.txt");
        
        var uploadResponse = await _client.PostAsync("/api/public/files", uploadContent);
        uploadResponse.EnsureSuccessStatusCode();
        
        var json = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>();
        string token = json.GetProperty("token").GetString()!;

        // 2. Téléchargement
        var downloadResponse = await _client.PostAsJsonAsync($"/api/public/files/{token}/download", new { });

        // Assert
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var fileBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        fileBytes.Should().HaveCount(3);
        fileBytes[0].Should().Be(99);
    }

    [Fact]
    public async Task Download_With_Invalid_Token_Should_Return_NotFound()
    {
        var response = await _client.PostAsJsonAsync("/api/public/files/INVALID_TOKEN_12345/download", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
