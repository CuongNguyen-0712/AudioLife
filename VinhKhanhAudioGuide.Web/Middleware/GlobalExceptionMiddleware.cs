using System.Net;
using System.Text.Json;

namespace VinhKhanhAudioGuide.Web.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred during request {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        // Default status code
        var statusCode = HttpStatusCode.InternalServerError;
        var message = "An internal server error occurred.";

        // You can customize status code based on exception type here
        // e.g., if (exception is UnauthorizedAccessException) statusCode = HttpStatusCode.Unauthorized;

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            StatusCode = context.Response.StatusCode,
            Message = message,
            Detailed = _env.IsDevelopment() ? exception.Message : null,
            StackTrace = _env.IsDevelopment() ? exception.StackTrace : null
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, options);

        await context.Response.WriteAsync(json);
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
