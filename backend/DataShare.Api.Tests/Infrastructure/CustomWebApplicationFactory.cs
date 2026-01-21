using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using DataShare.Api.Data;

namespace DataShare.Api.Tests.Infrastructure;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // IMPORTANT: remove aussi les configurers, sinon UseNpgsql + UseInMemory se cumulent
            services.RemoveAll<DbContextOptions<DataShareDbContext>>();
            services.RemoveAll<IConfigureOptions<DbContextOptions<DataShareDbContext>>>();
            services.RemoveAll<IDbContextOptionsConfiguration<DataShareDbContext>>();
            services.RemoveAll<DataShareDbContext>();

            services.AddDbContext<DataShareDbContext>(options =>
            {
                options.UseInMemoryDatabase("DataShare_Test_" + Guid.NewGuid());
            });

            // Init DB
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataShareDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
