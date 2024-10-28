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
        public IActionResult Login(string returnUrl = "http://localhost:5173/dashboard") //FRONT END TEAM: CHANGE THIS TO YOUR RETURN
        {
            _logger.LogInformation("Login initiated with returnUrl: {ReturnUrl}", returnUrl);

            // Validate the returnUrl to prevent open redirects
            if (!Url.IsLocalUrl(returnUrl) && !IsAllowedRedirectUrl(returnUrl))
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
        public IActionResult Logout(string returnUrl = "http://localhost:5173/")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("User {userID} logged out.", userId);
            return SignOut(new AuthenticationProperties { RedirectUri = returnUrl }, CookieAuthenticationDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Validates if the provided returnUrl is allowed.
        /// </summary>
        private bool IsAllowedRedirectUrl(string returnUrl)
        {
            var allowedUrls = new List<string>
            {
                "https://localhost:5173/dashboard",
                "https://localhost:5173/home",
            };

            return allowedUrls.Contains(returnUrl);
        }
    }
}