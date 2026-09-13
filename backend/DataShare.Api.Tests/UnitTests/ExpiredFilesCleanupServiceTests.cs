using DataShare.Api.Models;
using DataShare.Api.Services; 
using DataShare.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataShare.Api.Tests.UnitTests;

public class ExpiredFilesCleanupServiceTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Delete_Expired_Files_And_Keep_Valid_Ones()
    {
        // 1. Arrange
        var services = new ServiceCollection();
        
        var dbName = "TestDb_" + Guid.NewGuid().ToString();

        services.AddDbContext<DataShareDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: dbName));

        var mockStorage = new Mock<IFileStorage>();
        services.AddSingleton(mockStorage.Object);

        var mockLogger = new Mock<ILogger<ExpiredFilesCleanupService>>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        var expiredId = Guid.NewGuid();
        var validId = Guid.NewGuid();

        using (var scope = serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataShareDbContext>();
            db.Files.AddRange(
                new FileItem 
                { 
                    Id = expiredId, 
                    OriginalFileName = "expired.txt", 
                    StoredFileName = "expired_blob",
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1), // Expiré
                    Token = "tok1",
                    ContentType = "text/plain",
                    OwnerId = Guid.NewGuid()
                },
                new FileItem 
                { 
                    Id = validId, 
                    OriginalFileName = "valid.txt", 
                    StoredFileName = "valid_blob",
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(1), // Valide
                    Token = "tok2",
                    ContentType = "text/plain",
                    OwnerId = Guid.NewGuid()
                }
            );
            await db.SaveChangesAsync();
        }

        // 2. Act
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var service = new ExpiredFilesCleanupService(scopeFactory, mockLogger.Object); 

        await service.StartAsync(CancellationToken.None);
        
        await Task.Delay(500); 

        await service.StopAsync(CancellationToken.None);

        // 3. Assertion
        using (var scope = serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataShareDbContext>();
            
            var expiredFile = await db.Files.FindAsync(expiredId);
            Assert.Null(expiredFile);

            var validFile = await db.Files.FindAsync(validId);
            Assert.NotNull(validFile);
        }

        mockStorage.Verify(s => s.DeleteAsync("expired_blob", It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStorageFails_ShouldLogError_AndNotCrash()
    {
        // 1. Arrange
        var services = new ServiceCollection();
        services.AddDbContext<DataShareDbContext>(o => o.UseInMemoryDatabase("ErrorTestDb"));
        
        var mockStorage = new Mock<IFileStorage>();
        mockStorage
            .Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Storage Boom!"));

        services.AddSingleton(mockStorage.Object);

        var mockLogger = new Mock<ILogger<ExpiredFilesCleanupService>>();
        services.AddSingleton(mockLogger.Object);

        var sp = services.BuildServiceProvider();

        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataShareDbContext>();
            db.Files.Add(new FileItem 
            { 
                Id = Guid.NewGuid(), 
                StoredFileName = "path_bidon",
                OriginalFileName = "f.txt", 
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
                OwnerId = Guid.NewGuid(),
                ContentType = "text/plain",
                SizeBytes = 10,
                Token = "token_test_crash"
            });
            await db.SaveChangesAsync();
        }

        var service = ActivatorUtilities.CreateInstance<ExpiredFilesCleanupService>(sp);

        // 2. Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(100); // Laisser le temps au try/catch de s'exécuter
        await service.StopAsync(CancellationToken.None);

        // 3. Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }

}
