using System.Security.Claims;
using Ekomart.Application.Interfaces;

namespace Ekomart.Web.Middleware;

public class TechnicalLogMiddleware
{
    private static readonly TimeSpan LogTimeout = TimeSpan.FromSeconds(2);

    private readonly ILogger<TechnicalLogMiddleware> _logger;
    private readonly RequestDelegate _next;

    public TechnicalLogMiddleware(
        RequestDelegate next,
        ILogger<TechnicalLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITechnicalLogService technicalLogService)
    {
        try
        {
            await _next(context);

            if (context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
            {
                await WriteLogAsync(
                    context,
                    technicalLogService,
                    "Error",
                    $"HTTP {context.Response.StatusCode}",
                    details: null);
            }
        }
        catch (Exception exception)
        {
            await WriteLogAsync(
                context,
                technicalLogService,
                "Error",
                "Unhandled exception",
                exception.ToString());

            throw;
        }
    }

    private async Task WriteLogAsync(
        HttpContext context,
        ITechnicalLogService technicalLogService,
        string level,
        string message,
        string? details)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            timeout.CancelAfter(LogTimeout);

            await technicalLogService.LogAsync(
                level,
                message,
                GetPath(context),
                context.User.FindFirstValue(ClaimTypes.NameIdentifier),
                details,
                timeout.Token);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to write technical log.");
        }
    }

    private static string GetPath(HttpContext context)
    {
        return $"{context.Request.Method} {context.Request.Path}{context.Request.QueryString}";
    }
}
