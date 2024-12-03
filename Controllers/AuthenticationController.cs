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
        public IActionResult Login(string returnUrl = "http://localhost:5173/home") //FRONT END TEAM: CHANGE THIS TO YOUR RETURN
        {
            var redirectUri = "http://localhost:5173/authenticationPage";
            string userID = null;
            string userEmail = null;
            string firstName = null;
            string lastName = null;

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

                userID = user.UserID;
                userEmail = user.Email;
                firstName = user.FirstName;
                lastName = user.LastName;
            }

            if (userID != null)
            {
                redirectUri = $"{redirectUri}?userID={userID}?userEmail={userEmail}?firstName={firstName}?lastName={lastName}";
            }

            return Challenge(new AuthenticationProperties { RedirectUri = redirectUri }, GoogleDefaults.AuthenticationScheme);
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