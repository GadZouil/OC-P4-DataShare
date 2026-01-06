using System.Text;
using DataShare.Api.Data;
using DataShare.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using DataShare.Api.Services;
using Microsoft.AspNetCore.Http.Features;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "DataShare.Api", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header. Example: Bearer {token}"
    });

    // Swashbuckle 10.x : security requirement via OpenApiSecuritySchemeReference + document
    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        }
    );
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", policy =>
        policy.WithOrigins("http://localhost:5173") // Vite
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var conn = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<DataShareDbContext>(o => o.UseNpgsql(conn));

builder.Services
    .AddSingleton<IFileStorage, LocalFileStorage>()
    .AddIdentityCore<AppUser>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<DataShareDbContext>();

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 1_073_741_824; // 1 Go
});

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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("dev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
