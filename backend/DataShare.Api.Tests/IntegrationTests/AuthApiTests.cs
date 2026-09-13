using System.Net;
using System.Net.Http.Json;
using DataShare.Api.Tests.Helpers;
using FluentAssertions;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DataShare.Api.Tests.IntegrationTests;

public class AuthApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthApiTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

        [Fact]
    public async Task Scenario_Inscription_Et_Connexion_Reussie()
    {
        // 1. Inscription
        var registerData = new { Email = "user@test.com", Password = "P@ssword123!" };
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerData);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Connexion
        var loginData = new { Email = "user@test.com", Password = "P@ssword123!" };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginData);
        
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
        
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrWhiteSpace("l'API doit retourner un token JWT valide");
    }

    private class LoginResult
    {
        public string Token { get; set; } = "";
    }


    [Fact]
    public async Task Connexion_Avec_Mauvais_Mdp_Echoue()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new { Email = "fail@test.com", Password = "P@ssword123!" });

        var loginData = new { Email = "fail@test.com", Password = "WrongPassword!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginData);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Connexion_Email_Inconnu_Renvoie_401_Sans_Distinction()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = $"unknown_{Guid.NewGuid():N}@test.com", Password = "P@ssword123!" });

        // Même réponse qu'un mauvais mot de passe : pas d'énumération de comptes
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Inscription_Mot_De_Passe_Trop_Court_Renvoie_400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { Email = $"short_{Guid.NewGuid():N}@test.com", Password = "abc1234" }); // 7 caractères

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("PasswordTooShort");
    }

    [Fact]
    public async Task Inscription_Email_Deja_Utilise_Renvoie_400()
    {
        var email = $"dup_{Guid.NewGuid():N}@test.com";
        var first = await _client.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "P@ssword123!" });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await _client.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "P@ssword123!" });

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await second.Content.ReadAsStringAsync();
        body.Should().Contain("Duplicate");
    }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
