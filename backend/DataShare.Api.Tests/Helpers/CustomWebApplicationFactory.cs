using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using DataShare.Api.Data;
using DataShare.Api.Models;
using System;

namespace DataShare.Api.Tests.Infrastructure;

public class CustomWebApplicationFactory : CustomWebApplicationFactory<Program> { }

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("Jwt:Issuer", "DataShareTest");
        builder.UseSetting("Jwt:Audience", "DataShareTest");
        builder.UseSetting("Jwt:Key", "UneCleDeTestVraimentTresLonguePourEviterLesErreursDeSecuriteDeDotNet_123456789");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<DataShareDbContext>));
            services.AddDbContext<DataShareDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            });

            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DataShareDbContext>();
                db.Database.EnsureCreated();

                var testUserGuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

                if (!db.Users.Any(u => u.Id == testUserGuid))
                {
                    var newUser = new AppUser
                    {
                        Id = testUserGuid,
                        UserName = "TestUser",
                        Email = "test@datashare.com",
                        EmailConfirmed = true
                    };
                    
                    db.Users.Add(newUser);
                    db.SaveChanges();
                }
            }
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });
    }
}
