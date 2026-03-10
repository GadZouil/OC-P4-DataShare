using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Xunit;

namespace DataShare.Api.Tests.IntegrationTests;

public class PrivateFilesControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PrivateFilesControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> AuthenticateAsync()
    {
        var uniqueEmail = $"user_{Guid.NewGuid()}@test.com";
        var password = "P@ssword123!";

        await _client.PostAsJsonAsync("/api/auth/register", new { Email = uniqueEmail, Password = password });
        
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new { Email = uniqueEmail, Password = password });
        var json = await loginRes.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        return json.GetProperty("token").GetString()!;
    }

    [Fact]
    public async Task Delete_NonExistent_File_Returns_NotFound()
    {
        // 1. S'authentifier
        string token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 2. Tenter de supprimer un ID bidon
        var fakeId = Guid.NewGuid();
        var response = await _client.DeleteAsync($"/api/files/{fakeId}");

        // 3. Vérifier que l'API répond bien 404 (Not Found)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns_NotFound()
    {
        string token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var fakeId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/files/{fakeId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Upload_And_GetMine_Should_Return_List()
    {
        string token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Upload d'un fichier
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        content.Add(fileContent, "file", "test-private.txt");
        
        var uploadRes = await _client.PostAsync("/api/files", content);
        uploadRes.EnsureSuccessStatusCode();

        // Appel de GetMine
        var listRes = await _client.GetAsync("/api/files"); 
        
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var files = await listRes.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        
        // Vérifie qu'on a bien reçu une liste (tableau JSON)
        files.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
        files.GetArrayLength().Should().BeGreaterThan(0);
    }
}
