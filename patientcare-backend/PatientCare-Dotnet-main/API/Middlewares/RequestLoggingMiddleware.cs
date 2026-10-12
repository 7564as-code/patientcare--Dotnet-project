using System.Diagnostics;
using API.Extensions;

namespace API.Middlewares;

/// <summary>
/// רושם בלוג כל בקשה: שיטה, נתיב, קוד תשובה, זמן ומי המשתמש.
/// רץ מחוץ ל-ExceptionHandling כדי לראות את קוד התשובה הסופי (גם 500).
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var status = context.Response.StatusCode;
            var level = status >= 500 ? LogLevel.Error : status >= 400 ? LogLevel.Warning : LogLevel.Information;
            var caregiverId = context.User.TryGetCaregiverId();

            _logger.Log(
                level,
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms (caregiver: {CaregiverId})",
                context.Request.Method,
                context.Request.Path.Value,
                status,
                stopwatch.ElapsedMilliseconds,
                caregiverId?.ToString() ?? "anonymous");
        }
    }
}
