using Crossword.Application.Common;
using Crossword.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Crossword.Api.Infrastructure;

/// <summary>Turns application exceptions into RFC 7807 problem responses.</summary>
internal sealed class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title, errors) = exception switch
        {
            ValidationException v => (StatusCodes.Status400BadRequest, "Geçersiz istek",
                v.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "İstek gövdesi okunamadı", null),
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message, null),
            DomainException => (StatusCodes.Status409Conflict, exception.Message, null),
            _ => (StatusCodes.Status500InternalServerError, "Beklenmeyen bir hata oluştu", (Dictionary<string, string[]>?)null),
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception");

        context.Response.StatusCode = status;
        var problem = errors is null
            ? new ProblemDetails { Status = status, Title = title }
            : new ValidationProblemDetails(errors) { Status = status, Title = title };

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
