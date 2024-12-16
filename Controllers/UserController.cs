using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UserController(ILogger<UserController> logger) : ControllerBase
    {
        /// <summary>
        /// Retrieves the authenticated user's information.
        /// </summary>
        [HttpGet("userinfo")]
        public IActionResult GetUserInfo()
        {
            if (HttpContext.User.Identity is ClaimsIdentity { IsAuthenticated: true } identity)
            {
                var user = new
                {
                    FirstName = identity.FindFirst(ClaimTypes.GivenName)?.Value,
                    LastName = identity.FindFirst(ClaimTypes.Surname)?.Value,
                    Email = identity.FindFirst(ClaimTypes.Email)?.Value,
                    UserID = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value
                };

                logger.LogInformation("User info retrieved for UserID {UserID}", user.UserID);

                return Ok(user);
            }

            logger.LogWarning("Unauthorized access to GetUserInfo");

            return Unauthorized(new { message = "User is not authenticated." });
        }
    }
}