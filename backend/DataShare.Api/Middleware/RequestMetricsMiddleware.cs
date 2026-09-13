using System.Diagnostics;

namespace DataShare.Api.Middleware;

/// <summary>
/// Journalise, pour chaque requête HTTP, une ligne de log structurée contenant
/// les métriques clés : méthode, route, code de statut, durée (ms) et volume
/// transféré (octets, d'après les en-têtes Content-Length). Les placeholders
/// nommés ({Method}, {ElapsedMs}...) sont capturés comme propriétés par le
/// logger : ils sont exploitables tels quels par un collecteur de logs
/// (Seq, ELK, Grafana Loki...) ou par un simple grep.
/// </summary>
public sealed class RequestMetricsMiddleware
{
    private const string MessageTemplate =
        "HTTP {Method} {Path} -> {StatusCode} in {ElapsedMs} ms (request {RequestBytes} B, response {ResponseBytes} B)";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestMetricsMiddleware> _logger;

    public RequestMetricsMiddleware(RequestDelegate next, ILogger<RequestMetricsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? error = null;
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            var elapsedMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 1);
            // Une exception non gérée deviendra un 500 une fois le pipeline déroulé :
            // on la journalise comme telle, avec sa trace.
            var statusCode = error is null ? context.Response.StatusCode : StatusCodes.Status500InternalServerError;
            var level = statusCode >= 500 ? LogLevel.Error
                      : statusCode >= 400 ? LogLevel.Warning
                      : LogLevel.Information;

            _logger.Log(level, error, MessageTemplate,
                context.Request.Method,
                context.Request.Path.Value,
                statusCode,
                elapsedMs,
                context.Request.ContentLength ?? 0,
                context.Response.ContentLength ?? 0);
        }
    }
}

public static class RequestMetricsMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestMetrics(this IApplicationBuilder app)
        => app.UseMiddleware<RequestMetricsMiddleware>();
}
