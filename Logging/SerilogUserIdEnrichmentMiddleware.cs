
using System.Security.Claims;
using Serilog.Context;

namespace API_Backend.Logging
{
    /// <summary>
    /// Middleware to enrich Serilog logs with UserID from the HttpContext.
    /// </summary>
    public class SerilogUserIdEnrichmentMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SerilogUserIdEnrichmentMiddleware> _logger;

        public SerilogUserIdEnrichmentMiddleware(RequestDelegate next, ILogger<SerilogUserIdEnrichmentMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation("Enriching log context with UserID: {UserID}", userId);
                // Push the UserID into the Serilog LogContext
                using (LogContext.PushProperty("UserID", userId))
                {
                    await _next(context);
                }
            }
            else
            {
                _logger.LogWarning("UserID not found in claims. Logs will not be enriched with UserID.");
                await _next(context);
            }
        }
    }
}