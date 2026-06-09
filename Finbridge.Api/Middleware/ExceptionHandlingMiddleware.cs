using System.Net;
using System.Text.Json;
using Finbridge.Domain.Users.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Finbridge.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
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
            _logger.LogError(ex, "Необработанное исключение при выполнении запроса.");
            if (!context.Response.HasStarted)
            {
                await HandleExceptionAsync(context, ex, _env);
            }
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, IHostEnvironment env)
    {
        var code = exception switch
        {
            UserNotFoundException => HttpStatusCode.NotFound,
            KeyNotFoundException => HttpStatusCode.NotFound,
            NegativeBalanceException => HttpStatusCode.BadRequest,
            BalanceLimitExceededException => HttpStatusCode.BadRequest,
            ConcurrencyConflictException => HttpStatusCode.Conflict,
            ArgumentException => HttpStatusCode.BadRequest,
            InvalidOperationException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        var payload = new
        {
            error = new
            {
                message = exception.Message,
                detail = env.IsDevelopment() ? exception.InnerException?.Message : null,
                type = exception.GetType().Name
            }
        };

        var result = JsonSerializer.Serialize(payload);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        return context.Response.WriteAsync(result);
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
