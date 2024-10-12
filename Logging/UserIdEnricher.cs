

using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;
using System.Security.Claims;

namespace API_Backend.Logging
{
    /// <summary>
    /// Enricher to add the UserID to Serilog log events.
    /// </summary>
    public class UserIdEnricher : ILogEventEnricher
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserIdEnricher(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null && httpContext.User.Identity.IsAuthenticated)
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserID", userId));
                }
            }
        }
    }
}