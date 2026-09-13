using DataShare.Api.Data;
using DataShare.Api.Middleware;
using DataShare.Api.Models;
using DataShare.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Logs structurés : en dehors du développement (ex. conteneur Docker), chaque
// ligne de log est émise en JSON (timestamp, niveau, catégorie, message et
// propriétés nommées) pour être exploitable par un collecteur de logs.
// En développement, le format texte lisible de la console est conservé.
// ---------------------------------------------------------------------------
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole(o =>
    {
        o.IncludeScopes = false;
        o.TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
        o.UseUtcTimestamp = true;
    });
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---------------------------------------------------------------------------
// CORS : seules les origines listées dans Cors:AllowedOrigins sont acceptées
// (par défaut le serveur de dev Vite). En Docker, nginx sert le front et
// proxifie /api en même origine : CORS n'entre alors pas en jeu.
// Content-Disposition est exposé pour que le front lise le nom du fichier.
// ---------------------------------------------------------------------------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (allowedOrigins is null || allowedOrigins.Length == 0)
    allowedOrigins = new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Content-Disposition");
    });
});

// On charge PostgreSQL uniquement si on n'est PAS en test.
if (!builder.Environment.IsEnvironment("Testing"))
{
    var conn = builder.Configuration.GetConnectionString("Default");
    builder.Services.AddDbContext<DataShareDbContext>(o => o.UseNpgsql(conn)); // --> injection de dépendance
}

builder.Services
    .AddSingleton<IFileStorage, LocalFileStorage>()
    .AddIdentityCore<AppUser>(opt =>
    {
        opt.User.RequireUniqueEmail = true;
        opt.Password.RequiredLength = 8;
        opt.Password.RequireDigit = false;
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequireUppercase = false;
        opt.Password.RequireLowercase = false;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<DataShareDbContext>();

builder.Services.AddHostedService<ExpiredFilesCleanupService>();

builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 1_073_741_824);

// ---------------------------------------------------------------------------
// JWT : la clé de signature HMAC-SHA256 doit faire au moins 32 caractères
// (256 bits). On échoue au démarrage plutôt que de tourner avec une clé faible.
// ---------------------------------------------------------------------------
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException(
        "Configuration invalide : 'Jwt:Key' doit être définie et contenir au moins 32 caractères (variable d'environnement Jwt__Key ou user-secrets).");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DataShareDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Métriques par requête (méthode, route, statut, durée, volume) : placé en tête
// du pipeline pour mesurer le temps total, y compris l'authentification.
app.UseRequestMetrics();

app.UseRouting();
app.UseCors("frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program { }
