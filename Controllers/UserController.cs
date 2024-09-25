using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        /// <summary>
        /// Retrieves the authenticated user's information.
        /// </summary>
        [HttpGet("userinfo")]
        public IActionResult GetUserInfo()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity != null)
            {
                var user = new
                {
                    FirstName = identity.FindFirst(ClaimTypes.GivenName)?.Value,
                    LastName = identity.FindFirst(ClaimTypes.Surname)?.Value,
                    Email = identity.FindFirst(ClaimTypes.Email)?.Value,
                    UserID = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value
                };

                return Ok(user);
            }

            return Unauthorized();
        }
    }
}