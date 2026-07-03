using System.Net;
using System.Text.Json;
using LitXus.Application.Common.Exceptions;
using LitXus.Domain.Common;
using LitXus.Domain.Modules.Accounting.Exceptions;
using ValidationException = LitXus.Application.Common.Exceptions.ValidationException;

namespace LitXus.Api.Middleware;

/// <summary>Maps exceptions to the response envelope/status codes in docs/03_API_Specification.md §3.1-3.2.</summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, code, message, details) = exception switch
        {
            ValidationException ve => (
                HttpStatusCode.BadRequest,
                "VALIDATION_FAILED",
                "One or more fields are invalid.",
                (object?)ve.Errors.SelectMany(e => e.Value.Select(m => new { field = e.Key, message = m }))),

            NotFoundException nf => (HttpStatusCode.NotFound, "NOT_FOUND", nf.Message, null),

            ForbiddenException fb => (HttpStatusCode.Forbidden, "FORBIDDEN", fb.Message, null),

            AccountCodeDuplicateException dup => (HttpStatusCode.Conflict, dup.ErrorCode, dup.Message, null),
            StatementLineAlreadyMatchedException dup => (HttpStatusCode.Conflict, dup.ErrorCode, dup.Message, null),
            TaxCodeDuplicateException dup => (HttpStatusCode.Conflict, dup.ErrorCode, dup.Message, null),
            GLEntryLineAlreadyMatchedException dup => (HttpStatusCode.Conflict, dup.ErrorCode, dup.Message, null),

            VoidRequiresReasonException vr => (HttpStatusCode.BadRequest, vr.ErrorCode, vr.Message, null),

            DomainException de => (HttpStatusCode.UnprocessableEntity, de.ErrorCode, de.Message, null),

            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.", null),
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception");
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = JsonSerializer.Serialize(new
        {
            error = new { code, message, details },
        });

        await context.Response.WriteAsync(payload);
    }
}
