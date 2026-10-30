using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PureFreshMilk.Application.Common;
using PureFreshMilk.Domain.Common;

namespace PureFreshMilk.Api.Hosting;

/// <summary>
/// Turns the exceptions that use cases raise on purpose into RFC 9457 Problem Details:
/// a broken rule is 400 (with the field it concerns, when there is one), something missing is 404,
/// and a clash with stored data is 409. Both 404 and 409 carry a stable <c>code</c> that clients can branch on.
/// Anything else is left to the default handler, which answers 500 without details.
/// </summary>
internal sealed class ApplicationExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            DomainException { Field: { } field } domain => new ValidationProblemDetails(
                new Dictionary<string, string[]> { [field] = [domain.Message] })
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Extensions = { ["code"] = "validation-failed" },
            },
            DomainException domain => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "A business rule was broken.",
                Detail = domain.Message,
                Extensions = { ["code"] = "rule-violated" },
            },
            NotFoundException notFound => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not found",
                Detail = notFound.Message,
                Extensions = { ["code"] = notFound.Code },
            },
            ConflictException conflict => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = conflict.Message,
                Extensions = { ["code"] = conflict.Code },
            },
            _ => null,
        };

        if (problem is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem,
        });
    }
}
