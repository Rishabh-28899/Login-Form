
using System.Security.Claims;

namespace Token2Core.Middleware
{
    public class UserIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UserIdMiddleware> _logger;

        public UserIdMiddleware(
            RequestDelegate next,
            ILogger<UserIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userId = context.User
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(userId, out int parsedUserId) &&
                    parsedUserId > 0)
                {
                    context.Items["UserId"] = parsedUserId;

                    _logger.LogInformation(
                        "User ID {UserId} identified for request {Path}",
                        parsedUserId,
                        context.Request.Path
                    );
                }
                else
                {
                    _logger.LogWarning(
                        "Invalid or missing User ID claim for request {Path}",
                        context.Request.Path
                    );
                }
            }

            await _next(context);
        }
    }
}


