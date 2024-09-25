// File: Authorization/AllowSwaggerAccessRequirement.cs
using Microsoft.AspNetCore.Authorization;

namespace API_Backend
{
    /// <summary>
    /// A custom authorization requirement that allows access if the request is from Swagger.
    /// </summary>
    public class AllowSwaggerAccessRequirement : IAuthorizationRequirement
    {
        // This class serves as a marker and does not require additional properties.
    }
}