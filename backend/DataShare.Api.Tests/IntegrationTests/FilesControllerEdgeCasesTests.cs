using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json; 
using FluentAssertions;
using Xunit;

namespace DataShare.Api.Tests.IntegrationTests;

public class FilesControllerEdgeCasesTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FilesControllerEdgeCasesTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Test");
    }

    [Fact]
    public async Task Upload_ForbiddenExtension_ReturnsBadRequest()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        
        // On simule un fichier .exe
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        content.Add(fileContent, "file", "malware.exe"); 

        // Act
        var response = await _client.PostAsync("/api/files", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_EmptyFile_ReturnsBadRequest()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        
        // Act
        var response = await _client.PostAsync("/api/files", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Download_NonExistentToken_ReturnsNotFound()
    {
        // Arrange
        var fakeToken = "CeTokenNExistePasDuTout123";

        // Act
        var response = await _client.GetAsync($"/api/public/files/{fakeToken}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
