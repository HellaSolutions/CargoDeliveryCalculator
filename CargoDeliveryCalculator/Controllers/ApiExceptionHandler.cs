using CargoDeliveryCalculator.Services;

namespace CargoDeliveryCalculator.Controllers;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Define status code and message based on exception type
        var (statusCode, title, log) = exception switch
        {
            FormatException or ArgumentException => (StatusCodes.Status400BadRequest, exception.Message, false),
            IndexUnavailableException or UnknownActiveCargoException => (StatusCodes.Status404NotFound, exception.Message, false),
            _ => (StatusCodes.Status500InternalServerError, "An internal server error occurred", true)
        };
        if (log)
        {
            logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);    
        }
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = !log ? exception.Message : "An internal server error occurred",
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        // Return true to signal that the exception has been handled
        return true;
    }
}