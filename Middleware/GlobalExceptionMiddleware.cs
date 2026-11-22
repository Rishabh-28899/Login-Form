
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Token2Core.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Continue to the next middleware
                await _next(context);
            }
            catch (Exception ex)
            {
                // Log complete exception details
                _logger.LogError(
                    ex,
                    "Unhandled exception occurred. Request: {Method} {Path}. TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier
                );

                // If response has already started, rethrow
                if (context.Response.HasStarted)
                {
                    throw;
                }

                // Clear existing response
                context.Response.Clear();

                context.Response.StatusCode =
                    (int)HttpStatusCode.InternalServerError;

                context.Response.ContentType =
                    "application/problem+json";

                // Create standard error response
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected server error occurred.",
                    Detail = "Please try again later.",
                    Instance = context.Request.Path
                };

                // Add trace ID for debugging
                problem.Extensions["traceId"] =
                    context.TraceIdentifier;

                // Return JSON response
                await context.Response.WriteAsJsonAsync(problem);
            }
        }
    }
}

