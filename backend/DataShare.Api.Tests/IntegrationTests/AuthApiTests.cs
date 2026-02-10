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
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
