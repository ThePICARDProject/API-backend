using API_Backend.Services;
using API_Backend.Services.FileProcessing;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using API_Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OAuth;
using API_Backend.Models;
using System.Reflection;



var builder = WebApplication.CreateBuilder(args);

// Add controllers to the service container.
builder.Services.AddControllers();

// Register services for API documentation and exploration.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Register custom services for dependency injection.
builder.Services.AddScoped<ExperimentService>();
builder.Services.AddScoped<DataVisualization>();
builder.Services.AddScoped<IDatasetService, DatasetService>();

// Register the background service.
builder.Services.AddHostedService<ExperimentWorkerService>();

// Register the dataset service
builder.Services.AddScoped<IDatasetService, DatasetService>();

// Register the DatasetService (assuming the DatasetService is correctly implemented)
builder.Services.AddScoped<IDatasetService, DatasetService>();

// Register the background service.
builder.Services.AddHostedService<ExperimentWorkerService>();

// Register DbContext with MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 26)) // Replace with your MySQL server version
    )
);

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    var googleAuthNSection = builder.Configuration.GetSection("Authentication:Google");

    options.ClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? googleAuthNSection["ClientId"];
    options.ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? googleAuthNSection["ClientSecret"];
    options.CallbackPath = googleAuthNSection["CallbackPath"];

    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");

    // Restrict access to users with a specific email domain.
    options.Events = new OAuthEvents
    {
        OnCreatingTicket = async context =>
        {
            var email = context.Identity.FindFirst(ClaimTypes.Email)?.Value;
            var firstName = context.Identity.FindFirst(ClaimTypes.GivenName)?.Value;
            var lastName = context.Identity.FindFirst(ClaimTypes.Surname)?.Value;

            // Check email domain
            if (email != null && !email.EndsWith("@mix.wvu.edu", StringComparison.OrdinalIgnoreCase))
            {
                context.Fail("Unauthorized email domain.");
                return;
            }

            // Retrieve or create user in the database
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                user = new User
                {
                    UserID = Guid.NewGuid().ToString(),
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync();
            }

            var claimsIdentity = (ClaimsIdentity)context.Principal.Identity;
            var existingNameIdClaims = claimsIdentity.FindAll(ClaimTypes.NameIdentifier).ToList();
            foreach (var claim in existingNameIdClaims)
            {
                claimsIdentity.RemoveClaim(claim);
            }

            // Add UserID as the NameIdentifier claim
            claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.UserID));
        }
    };
});

// Register IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Build the WebApplication.
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    // Enable Swagger for API documentation.
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();