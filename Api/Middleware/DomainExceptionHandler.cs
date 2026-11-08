using Microsoft.AspNetCore.Diagnostics;
using Studium_Catalog.Domain.Exceptions;

namespace Studium_Catalog.Api.Middleware;

public class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainException)
            return false;

        httpContext.Response.StatusCode = domainException is EntityNotFoundException
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;

        await httpContext.Response.WriteAsJsonAsync(new
        {
            error = domainException.ErrorCode,
            message = domainException.Message
        }, cancellationToken);

        return true;
    }
}
