using System.Net;
using System.Text.Json;
using CollaborativeWorkspace.Domain.Exceptions;

namespace CollaborativeWorkspace.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/problem+json";

        var (status, title, detail) = ex switch
        {
            NotFoundException nfe => (HttpStatusCode.NotFound, "Resource Not Found", nfe.Message),
            UnauthorizedException uae => (HttpStatusCode.Forbidden, "Forbidden", uae.Message),
            SubscriptionRequiredException sre => (HttpStatusCode.PaymentRequired, "Subscription Required", sre.Message),
            EmailNotVerifiedException enve => (HttpStatusCode.Unauthorized, "Email Not Verified", enve.Message),
            BusinessRuleException bre => (HttpStatusCode.BadRequest, "Business Rule Violation", bre.Message),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error", "An unexpected error occurred.")
        };

        context.Response.StatusCode = (int)status;

        var problem = new
        {
            type = $"https://tools.ietf.org/html/rfc7807",
            title,
            status = (int)status,
            detail,
            instance = context.Request.Path.Value
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
