using iDiski.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace iDiski.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
            await HandleExceptionAsync(context, exception);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ProblemDetails
        {
            Instance = context.Request.Path
        };

        switch (exception)
        {
            case ValidationException validationException:
                context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                response.Title = "One or more validation errors occurred.";
                response.Status = StatusCodes.Status422UnprocessableEntity;
                response.Detail = string.Join(", ", validationException.Errors.Select(e => e.ErrorMessage));
                break;

            case NotFoundException notFoundException:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                response.Title = "The requested resource was not found.";
                response.Status = StatusCodes.Status404NotFound;
                response.Detail = notFoundException.Message;
                break;

            case InvalidOperationException invalidOpException:
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                response.Title = "A conflict occurred.";
                response.Status = StatusCodes.Status409Conflict;
                response.Detail = invalidOpException.Message;
                break;

            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Title = "An internal server error occurred.";
                response.Status = StatusCodes.Status500InternalServerError;
                response.Detail = "An unexpected error has occurred. Please try again later.";
                break;
        }

        return context.Response.WriteAsJsonAsync(response);
    }
}
