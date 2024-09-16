using API_backend.Services.DataVisualization;
using API_backend.Services.FileProcessing;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;

// Create a WebApplication builder instance.
var builder = WebApplication.CreateBuilder(args);

// Add controllers to the service container.
builder.Services.AddControllers();

// Register services for API documentation and exploration.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register custom services for dependency injection.
builder.Services.AddSingleton<DataVisualization>();


// Configure authentication using Google OAuth 2.0.
builder.Services.AddAuthentication(options =>
{
    // Set the default authentication schemes.
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie() // Add cookie authentication.
.AddGoogle(options =>
{
    // Get Google authentication settings from configuration.
    var googleAuthNSection = builder.Configuration.GetSection("Authentication:Google");

    // Set the Client ID and Client Secret from environment variables or configuration.
    options.ClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? googleAuthNSection["ClientId"];
    options.ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? googleAuthNSection["ClientSecret"];
    options.CallbackPath = googleAuthNSection["CallbackPath"];

    // Specify the scopes required.
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");

    // Restrict access to users with WVU email domain.
    options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
    {
        OnCreatingTicket = context =>
        {
            // Retrieve the user's email from the claims.
            var email = context.Identity.FindFirst(ClaimTypes.Email)?.Value;

            // Check if the email ends with '@mix.wvu.edu'.
            if (email != null && !email.EndsWith("@mix.wvu.edu", StringComparison.OrdinalIgnoreCase))
            {
                // Fail the authentication if the email domain is not allowed.
                context.Fail("Unauthorized email domain.");
            }
            return Task.CompletedTask;
        }
    };
});

// Build the WebApplication.
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Use the developer exception page in development environment.
    app.UseDeveloperExceptionPage();

    // Enable Swagger for API documentation.
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enforce HTTPS redirection.
app.UseHttpsRedirection();

// Add authentication and authorization middleware to the pipeline.
app.UseAuthentication();
app.UseAuthorization();

// Map controller routes.
app.MapControllers();

// Run the application.
app.Run();