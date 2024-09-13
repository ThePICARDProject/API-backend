using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using API_backend.Models;
using API_backend.Services;
using API_backend.Services.Token;

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly ITokenService _tokenService;

        public AuthenticationController(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        [HttpPost("authenticate")]
        public IActionResult Authenticate([FromBody] TokenRequest request)
        {
            if (_tokenService.ValidateToken(request.Token, out var validatedToken))
            {
                var jwtToken   = validatedToken as JwtSecurityToken;
                var userClaims = jwtToken.Claims;

                var user = new User
                {
                    FirstName = userClaims.FirstOrDefault(c => c.Type == "given_name")?.Value,
                    LastName  = userClaims.FirstOrDefault(c => c.Type == "family_name")?.Value,
                    Email     = userClaims.FirstOrDefault(c => c.Type == "email")?.Value
                };

                return Ok(user);
            }

            return Unauthorized();
        }
    }
}