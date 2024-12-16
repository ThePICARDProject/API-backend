
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace API_Backend
{
    /// <summary>
    /// Authorization handler that allows access if the request is from Swagger UI.
    /// </summary>
    public class AllowSwaggerAccessHandler : AuthorizationHandler<AllowSwaggerAccessRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _env;

        public AllowSwaggerAccessHandler(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env)
        {
            _httpContextAccessor = httpContextAccessor;
            _env = env;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AllowSwaggerAccessRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var path = httpContext.Request.Path;

                // Allow access if the request is to Swagger and in Development environment
                if (_env.IsDevelopment() && path.StartsWithSegments("/swagger"))
                {
                    context.Succeed(requirement);
                }
                // Otherwise, require authenticated user
                else if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}