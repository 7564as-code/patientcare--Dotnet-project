using System.Net;
using System.Text.Json;
using PatientCare.Core.Exceptions;

namespace API.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // הלקוח ביטל את הבקשה. זה לא שגיאה.
            _logger.LogInformation("Request {Method} {Path} was cancelled by the client.", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(ex, "Unhandled exception after the response started: {Method} {Path}", context.Request.Method, context.Request.Path);
                throw;
            }

            var (statusCode, message) = Classify(ex);

            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
            }
            else
            {
                _logger.LogWarning(ex, "Request {Method} {Path} failed with {StatusCode}", context.Request.Method, context.Request.Path, (int)statusCode);
            }

            await WriteErrorResponseAsync(context, statusCode, message);
        }
    }

    private static (HttpStatusCode, string) Classify(Exception ex) => ex switch
    {
        UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "אין לך הרשאה לבצע את הפעולה."),
        ConcurrencyException => (HttpStatusCode.Conflict, "הנתונים שונו על ידי משתמש אחר. רענן ונסה שוב."),
        DataConstraintException => (HttpStatusCode.Conflict, "הפעולה נכשלה בגלל הפרת אילוץ בנתונים."),
        ArgumentException => (HttpStatusCode.BadRequest, ex.Message),
        _ => (HttpStatusCode.InternalServerError, "אירעה שגיאה לא צפויה.")
    };

    private static Task WriteErrorResponseAsync(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var correlationId = context.Response.Headers.TryGetValue(CorrelationIdMiddleware.HeaderName, out var id)
            ? id.ToString()
            : null;

        var payload = JsonSerializer.Serialize(new
        {
            status = (int)statusCode,
            message,
            correlationId,
            timestamp = DateTime.UtcNow
        });

        return context.Response.WriteAsync(payload);
    }
}
