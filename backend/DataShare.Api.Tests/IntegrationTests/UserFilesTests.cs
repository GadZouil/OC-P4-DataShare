using System.Net;
using System.Net.Http.Json;
using DataShare.Api.Data;
using DataShare.Api.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Net.Http.Headers;

namespace DataShare.Api.Tests.IntegrationTests;

public class UserFilesTests : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public UserFilesTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Test");
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
    public async Task Get_MyFiles_Should_Return_Empty_List_Initially()
    {
        var response = await _client.GetAsync("/api/files");
        response.EnsureSuccessStatusCode();
        
        var files = await response.Content.ReadFromJsonAsync<List<dynamic>>();
        files.Should().BeEmpty();
    }

    [Fact]
    public async Task Upload_File_As_User_Should_Appear_In_MyFiles()
    {
        // 1. Upload
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 10, 20, 30 });
        content.Add(fileContent, "file", "user_file.txt");
        
        var uploadResponse = await _client.PostAsync("/api/files", content);
        
        // Assert
        uploadResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        uploadResponse.EnsureSuccessStatusCode();

        // 2. Vérification
        var myFilesResponse = await _client.GetAsync("/api/files");
        myFilesResponse.EnsureSuccessStatusCode();

        var json = await myFilesResponse.Content.ReadAsStringAsync();
        json.Should().Contain("user_file.txt");
    }
}
