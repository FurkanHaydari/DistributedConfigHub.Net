using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DistributedConfigHub.Application.Exceptions;
using FluentValidation; 
using ValidationException = DistributedConfigHub.Application.Exceptions.ValidationException;

namespace DistributedConfigHub.Api.Infrastructure.ExceptionHandling;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1. Always log the error
        logger.LogError(exception, "An exception occurred: {Message}", exception.Message);

        // 2. Determine HTTP Status Code and Message based on error type
        var (statusCode, title, detail) = exception switch
        {
            // Error thrown when record is not found in Update/Delete operations
            KeyNotFoundException => 
                (StatusCodes.Status404NotFound, "Not Found", exception.Message),

            // Tenant Isolation (Security) violation
            UnauthorizedAccessException => 
                (StatusCodes.Status403Forbidden, "Forbidden", exception.Message),

            // Duplicate (Conflict) situation: "This record already exists" -> 409 Conflict
            InvalidOperationException ex when ex.Message.Contains("already exists") => 
                (StatusCodes.Status409Conflict, "Conflict", exception.Message),

            // Other Business and Domain Validation errors (e.g., Type and Value mismatch)
            InvalidOperationException => 
                (StatusCodes.Status400BadRequest, "Bad Request", exception.Message),

            // Cases such as sending empty parameters
            ArgumentException => 
                (StatusCodes.Status400BadRequest, "Bad Request", exception.Message),

            // MediatR Pipeline + FluentValidation errors
            ValidationException valEx => 
                (StatusCodes.Status400BadRequest, "Validation Failed", "One or more validation errors occurred in the request data."),

            // If none of the above (An unexpected error occurred)
            _ => 
                (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred while processing your request. Please check the logs.")
        };

        // 3. Create ProblemDetails object
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        // 4. If it's a ValidationException, add errors to the list (format expected by Frontend)
        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions.Add("errors", validationException.Errors);
        }

        // 4. Write response to Client
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}