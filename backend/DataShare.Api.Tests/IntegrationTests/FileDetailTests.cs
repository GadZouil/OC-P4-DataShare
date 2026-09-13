using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DataShare.Api.Data;
using DataShare.Api.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DataShare.Api.Tests.IntegrationTests;

public class FileDetailTests : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public FileDetailTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Test");
    }

    public async Task InitializeAsync()
    {
        // Nettoyage de la db avant chaque test
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataShareDbContext>();
        db.Files.RemoveRange(db.Files);
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> UploadTestFileAsync(string filename = "test.txt")
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        
        content.Add(fileContent, "file", filename);
        
        var response = await _client.PostAsync("/api/files", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        string idStr = result.GetProperty("id").GetString()!;
        
        return Guid.Parse(idStr);
    }

    [Fact]
    public async Task GetById_Should_Return_Correct_Metadata()
    {
        // Arrange
        var fileId = await UploadTestFileAsync("mon_cv.pdf");

        // Act
        var response = await _client.GetAsync($"/api/files/{fileId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        string originalName = json.GetProperty("originalFileName").GetString()!;
        originalName.Should().Be("mon_cv.pdf");
    }

    [Fact]
    public async Task GetById_With_Wrong_Id_Should_Return_NotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/files/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Should_Remove_File()
    {
        // Arrange
        var fileId = await UploadTestFileAsync("to_delete.txt");

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/files/{fileId}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent); // 204

        // Vérification que le fichier n'existe plus
        var getResponse = await _client.GetAsync($"/api/files/{fileId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Unknown_File_Should_Return_NotFound()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/files/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
