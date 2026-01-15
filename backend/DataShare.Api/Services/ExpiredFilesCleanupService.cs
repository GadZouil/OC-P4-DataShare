using DataShare.Api.Data;
using DataShare.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace DataShare.Api.Services;

public sealed class ExpiredFilesCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredFilesCleanupService> _logger;

    public ExpiredFilesCleanupService(IServiceScopeFactory scopeFactory, ILogger<ExpiredFilesCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 1 run au démarrage, puis toutes les 24h
        await PurgeOnce(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PurgeOnce(stoppingToken);
        }
    }

    private async Task PurgeOnce(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataShareDbContext>();
            var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

            var now = DateTimeOffset.UtcNow;

            var expired = await db.Files
                .Where(f => f.ExpiresAt <= now)
                .ToListAsync(ct);

            if (expired.Count == 0) return;

            foreach (var f in expired)
            {
                try
                {
                    await storage.DeleteAsync(f.StoredFileName, ct);
                }
                catch (Exception ex)
                {
                    // Si le fichier n'existe plus physiquement, on continue quand même la purge DB
                    _logger.LogWarning(ex, "Failed to delete stored file {StoredFileName}", f.StoredFileName);
                }
            }

            db.Files.RemoveRange(expired);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Purged {Count} expired files", expired.Count);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // normal à l'arrêt
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Expired files purge failed");
        }
    }
}
