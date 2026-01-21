using DataShare.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DataShare.Api.Tests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // Retire la config DB réelle (Npgsql etc.)
            services.RemoveAll(typeof(DbContextOptions<DataShareDbContext>));
            services.RemoveAll<DataShareDbContext>();

            // Remplace par une DB mémoire
            services.AddDbContext<DataShareDbContext>(options =>
            {
                options.UseInMemoryDatabase("DataShare_TestDb");
            });

            // Crée la DB
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataShareDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
