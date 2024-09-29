
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;

namespace API_Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(ILogger<AuthenticationController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Initiates the Google OAuth 2.0 authentication process.
        /// </summary>
        /// <param name="returnUrl">The URL to redirect to after successful authentication.</param>
        [HttpGet("login")]
        public IActionResult Login(string returnUrl = "/swagger/index.html")
        {
            _logger.LogInformation("Login initiated with returnUrl: {ReturnUrl}", returnUrl);

            // Ensure the returnUrl is local to prevent open redirects
            if (!Url.IsLocalUrl(returnUrl))
            {
                _logger.LogWarning("Invalid return URL: {ReturnUrl}", returnUrl);
                return BadRequest("Invalid return URL.");
            }

            return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, GoogleDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Logs the user out and clears the authentication cookie.
        /// </summary>
        [HttpGet("logout")]
        public IActionResult Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("User {userID} logged out.", userId);
            return SignOut(new AuthenticationProperties { RedirectUri = "/swagger/index.html" }, CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}