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
        // 1. Hatayı her halükarda logla
        logger.LogError(exception, "An exception occurred: {Message}", exception.Message);

        // 2. Gelen hatanın tipine göre HTTP Status Kodunu ve Mesajı belirle
        var (statusCode, title, detail) = exception switch
        {
            // Update/Delete işlemlerinde kayıt bulunamazsa fırlatılan hata
            KeyNotFoundException => 
                (StatusCodes.Status404NotFound, "Not Found", exception.Message),

            // Tenant Isolation (Güvenlik) ihlali
            UnauthorizedAccessException => 
                (StatusCodes.Status403Forbidden, "Forbidden", exception.Message),

            // Duplicate (Çakışma) durumu: "Bu kayıt zaten var" -> 409 Conflict
            InvalidOperationException ex when ex.Message.Contains("already exists") => 
                (StatusCodes.Status409Conflict, "Conflict", exception.Message),

            // Diğer Business ve Domain Validation hataları (Örn: Type ve Value uyuşmazlığı)
            InvalidOperationException => 
                (StatusCodes.Status400BadRequest, "Bad Request", exception.Message),

            // Boş parametre gönderilmesi gibi durumlar
            ArgumentException => 
                (StatusCodes.Status400BadRequest, "Bad Request", exception.Message),

            // MediatR Pipeline + FluentValidation hataları
            ValidationException valEx => 
                (StatusCodes.Status400BadRequest, "Validation Failed", "One or more validation errors occurred in the request data."),

            // Yukarıdakilerin hiçbiri değilse (Beklenmedik bir hata oluştuysa)
            _ => 
                (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred while processing your request. Please check the logs.")
        };

        // 3. ProblemDetails nesnesini oluştur
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        // 4. Eğer bir ValidationException ise, hataları listeye ekle (Frontend'in beklediği format)
        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions.Add("errors", validationException.Errors);
        }

        // 4. İstemciye (Client) cevabı yaz
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}