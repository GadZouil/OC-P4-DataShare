using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DataShare.Api.Tests.IntegrationTests;

public class PublicFilesControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PublicFilesControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private MultipartFormDataContent CreateFileContent(string fileName, byte[] content, string? password = null, int? expiresInDays = null)
    {
        var formData = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        formData.Add(fileContent, "file", fileName);

        if (password != null)
        {
            formData.Add(new StringContent(password), "password");
        }

        if (expiresInDays.HasValue)
        {
            formData.Add(new StringContent(expiresInDays.Value.ToString()), "expiresInDays");
        }

        return formData;
    }

    [Fact]
    public async Task Upload_Anonymous_Should_Succeed()
    {
        var content = CreateFileContent("test.txt", new byte[] { 1, 2, 3 });
        var response = await _client.PostAsync("/api/public/files", content);
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        json.TryGetProperty("token", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Upload_With_Password_Should_Return_Token()
    {
        var content = CreateFileContent("secret.txt", new byte[] { 1, 2, 3 }, "Secret123!");
        var response = await _client.PostAsync("/api/public/files", content);
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Upload_With_Invalid_Expiration_Should_Return_BadRequest()
    {
        // Expiration à 0 ou négative
        var content = CreateFileContent("test.txt", new byte[] { 1 }, null, 0);
        var response = await _client.PostAsync("/api/public/files", content);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_With_Short_Password_Should_Return_BadRequest()
    {
        var content = CreateFileContent("test.txt", new byte[] { 1 }, "123"); 
        var response = await _client.PostAsync("/api/public/files", content);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Download_With_Valid_Token_Should_Return_File()
    {
        // 1. Upload
        var content = CreateFileContent("image.png", new byte[] { 10, 20, 30 });
        var uploadRes = await _client.PostAsync("/api/public/files", content);
        uploadRes.EnsureSuccessStatusCode(); 

        var json = await uploadRes.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        string token = json.GetProperty("token").GetString()!;

        // 2. Download
        var downloadReq = new { };
        var response = await _client.PostAsJsonAsync($"/api/public/files/{token}/download", downloadReq);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var fileBytes = await response.Content.ReadAsByteArrayAsync();
        fileBytes.Should().ContainInOrder(10, 20, 30);
    }

    [Fact]
    public async Task Download_Protected_File_Without_Password_Returns_Unauthorized()
    {
        // 1. Upload avec password
        var content = CreateFileContent("secret.txt", new byte[] { 1 }, "Secret123!");
        var uploadRes = await _client.PostAsync("/api/public/files", content);
        uploadRes.EnsureSuccessStatusCode();

        var json = await uploadRes.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        string token = json.GetProperty("token").GetString()!;

        // 2. Tentative de download sans password (corps vide)
        var downloadReq = new { }; 
        var response = await _client.PostAsJsonAsync($"/api/public/files/{token}/download", downloadReq);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Download_Protected_File_With_Wrong_Password_Returns_Unauthorized()
    {
        // 1. Upload
        var content = CreateFileContent("secret.txt", new byte[] { 1 }, "Secret123!");
        var uploadRes = await _client.PostAsync("/api/public/files", content);
        uploadRes.EnsureSuccessStatusCode();

        var json = await uploadRes.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        string token = json.GetProperty("token").GetString()!;

        // 2. Download avec mauvais password
        var downloadReq = new { password = "WrongPassword" };
        var response = await _client.PostAsJsonAsync($"/api/public/files/{token}/download", downloadReq);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Download_Protected_File_With_Correct_Password_Returns_File()
    {
        // 1. Upload
        var content = CreateFileContent("secret.txt", new byte[] { 65, 66, 67 }, "Secret123!");
        var uploadRes = await _client.PostAsync("/api/public/files", content);
        uploadRes.EnsureSuccessStatusCode();

        var json = await uploadRes.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        string token = json.GetProperty("token").GetString()!;

        // 2. Download avec bon password
        var downloadReq = new { password = "Secret123!" };
        var response = await _client.PostAsJsonAsync($"/api/public/files/{token}/download", downloadReq);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var fileBytes = await response.Content.ReadAsByteArrayAsync();
        fileBytes.Should().ContainInOrder(65, 66, 67);
    }

    [Fact]
    public async Task Download_NonExistentToken_ReturnsNotFound()
    {
        var fakeToken = Guid.NewGuid().ToString();
        var downloadReq = new { password = "pwd" };
        
        var response = await _client.PostAsJsonAsync($"/api/public/files/{fakeToken}/download", downloadReq);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

}
