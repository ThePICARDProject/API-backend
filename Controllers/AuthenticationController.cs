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
        /// <summary>
        /// Initiates the Google OAuth 2.0 authentication process.
        /// </summary>
        /// <param name="returnUrl">The URL to redirect to after successful authentication.</param>
        [HttpGet("login")]
        public IActionResult Login(string returnUrl = "/swagger/index.html")
        {
            // Ensure the returnUrl is local to prevent open redirects.
            if (!Url.IsLocalUrl(returnUrl))
            {
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
            return SignOut(new AuthenticationProperties { RedirectUri = "/swagger/index.html" }, CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}