using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace API_backend.Services.Token
{
    public interface ITokenService
    {
        bool ValidateToken(string token, out SecurityToken validatedToken);
    }

    public class TokenService(IConfiguration configuration) : ITokenService
    {
        private readonly IConfiguration _configuration = configuration;

        public bool ValidateToken(string token, out SecurityToken validatedToken)
        {
            validatedToken = null;
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = new SymmetricSecurityKey(key),
                    ValidateIssuer           = true,
                    ValidIssuer              = _configuration["Jwt:Issuer"],
                    ValidateAudience         = true,
                    ValidAudience            = _configuration["Jwt:Audience"],
                    ValidateLifetime         = true
                }, out validatedToken);

                return true;
            }
            catch (SecurityTokenException ex)
            {
                // Log exception (ex) here
                return false;
            }
        }
    }
}