using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataShare.Api.Tests.Infrastructure;

/// <summary>
/// Schéma d'authentification de test : remplace la validation JWT.
/// - Sans en-tête Authorization : requête anonyme.
/// - Avec Authorization : utilisateur de test par défaut (<see cref="DefaultUserId"/>).
/// - Avec en-tête <c>X-Test-UserId</c> : permet d'incarner un AUTRE utilisateur,
///   afin de tester l'isolation des données entre comptes.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string DefaultUserId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    public const string UserIdHeader = "X-Test-UserId";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // on authentifie SEULEMENT si le header Authorization est présent.
        // Si le client HTTP du test n'envoie pas de header Authorization, l'utilisateur sera Anonyme.
        if (!Context.Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = DefaultUserId;
        if (Context.Request.Headers.TryGetValue(UserIdHeader, out var headerValue)
            && Guid.TryParse(headerValue.ToString(), out var parsed))
        {
            userId = parsed.ToString();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, "TestUser"),
            new Claim(ClaimTypes.Email, "test@datashare.com")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
