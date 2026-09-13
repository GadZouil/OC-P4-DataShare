using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DataShare.Api.Data;
using DataShare.Api.Models;
using DataShare.Api.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DataShare.Api.Tests.IntegrationTests;

/// <summary>
/// Tests de sécurité : isolation des fichiers entre utilisateurs et protection
/// des endpoints privés. Utilise l'en-tête X-Test-UserId (voir TestAuthHandler)
/// pour incarner un second utilisateur.
/// </summary>
public class SecurityTests : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
{
    private const string OtherUserId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _owner;      // utilisateur de test par défaut (propriétaire)
    private readonly HttpClient _otherUser;  // second utilisateur authentifié
    private readonly HttpClient _anonymous;  // aucun en-tête Authorization

    public SecurityTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;

        _owner = factory.CreateClient();
        _owner.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        _otherUser = factory.CreateClient();
        _otherUser.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        _otherUser.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, OtherUserId);

        _anonymous = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataShareDbContext>();
        db.Files.RemoveRange(db.Files);
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<JsonElement> UploadAsync(HttpClient client, string fileName)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", fileName);

        var response = await client.PostAsync("/api/files", content);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Other_User_Cannot_Read_File_Metadata()
    {
        var uploaded = await UploadAsync(_owner, "private.txt");
        var id = uploaded.GetProperty("id").GetString();

        var response = await _otherUser.GetAsync($"/api/files/{id}");

        // 404 (et non 403) : on ne révèle pas l'existence du fichier
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Other_User_Cannot_Delete_File()
    {
        var uploaded = await UploadAsync(_owner, "keep-me.txt");
        var id = uploaded.GetProperty("id").GetString();

        var deleteResponse = await _otherUser.DeleteAsync($"/api/files/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Le fichier est toujours accessible par son propriétaire
        var stillThere = await _owner.GetAsync($"/api/files/{id}");
        stillThere.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Files_Me_Returns_Only_Own_Files()
    {
        await UploadAsync(_owner, "owner-file.txt");
        await UploadAsync(_otherUser, "other-file.txt");

        var ownerList = await _owner.GetFromJsonAsync<JsonElement>("/api/files/me");
        var otherList = await _otherUser.GetFromJsonAsync<JsonElement>("/api/files/me");

        ownerList.GetArrayLength().Should().Be(1);
        ownerList[0].GetProperty("originalFileName").GetString().Should().Be("owner-file.txt");

        otherList.GetArrayLength().Should().Be(1);
        otherList[0].GetProperty("originalFileName").GetString().Should().Be("other-file.txt");
    }

    [Fact]
    public async Task Files_Me_Status_Filter_Separates_Active_And_Expired()
    {
        await UploadAsync(_owner, "active.txt");

        // Un fichier expiré ne peut pas être créé via l'API (expiration min. 1 jour) :
        // on l'insère directement en base.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataShareDbContext>();
            db.Files.Add(new FileItem
            {
                OwnerId = Guid.Parse(TestAuthHandler.DefaultUserId),
                OriginalFileName = "expired.txt",
                StoredFileName = "expired_blob.txt",
                ContentType = "text/plain",
                SizeBytes = 3,
                Token = "expired-token-" + Guid.NewGuid().ToString("N"),
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-8),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }

        var all = await _owner.GetFromJsonAsync<JsonElement>("/api/files/me");
        var active = await _owner.GetFromJsonAsync<JsonElement>("/api/files/me?status=active");
        var expired = await _owner.GetFromJsonAsync<JsonElement>("/api/files/me?status=expired");

        all.GetArrayLength().Should().Be(2);

        active.GetArrayLength().Should().Be(1);
        active[0].GetProperty("originalFileName").GetString().Should().Be("active.txt");

        expired.GetArrayLength().Should().Be(1);
        expired[0].GetProperty("originalFileName").GetString().Should().Be("expired.txt");
    }

    [Fact]
    public async Task Anonymous_Requests_Are_Rejected_On_Private_Endpoints()
    {
        var uploaded = await UploadAsync(_owner, "private.txt");
        var id = uploaded.GetProperty("id").GetString();

        var list = await _anonymous.GetAsync("/api/files/me");
        var detail = await _anonymous.GetAsync($"/api/files/{id}");
        var delete = await _anonymous.DeleteAsync($"/api/files/{id}");

        list.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        detail.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        delete.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Expired_Link_Returns_410_Gone_For_Metadata_And_Download()
    {
        var token = "expired-" + Guid.NewGuid().ToString("N");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataShareDbContext>();
            db.Files.Add(new FileItem
            {
                OwnerId = Guid.Parse(TestAuthHandler.DefaultUserId),
                OriginalFileName = "old.txt",
                StoredFileName = "old_blob.txt",
                ContentType = "text/plain",
                SizeBytes = 3,
                Token = token,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-10),
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
            });
            await db.SaveChangesAsync();
        }

        var meta = await _anonymous.GetAsync($"/api/public/files/{token}");
        var download = await _anonymous.PostAsJsonAsync($"/api/public/files/{token}/download", new { password = (string?)null });

        meta.StatusCode.Should().Be(HttpStatusCode.Gone);
        download.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    [Fact]
    public async Task Public_Link_Does_Not_Expose_Owner_Or_Token()
    {
        var uploaded = await UploadAsync(_owner, "shared.txt");
        var token = uploaded.GetProperty("token").GetString();

        var meta = await _anonymous.GetFromJsonAsync<JsonElement>($"/api/public/files/{token}");

        meta.GetProperty("originalFileName").GetString().Should().Be("shared.txt");
        meta.TryGetProperty("ownerId", out _).Should().BeFalse();
        meta.TryGetProperty("token", out _).Should().BeFalse();
        meta.TryGetProperty("storedFileName", out _).Should().BeFalse();
    }
}
