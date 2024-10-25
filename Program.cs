using API_Backend.Services.Docker_Swarm;
using API_Backend.Services.DataVisualization;
using API_Backend.Data;
using API_Backend.Models;
using API_Backend.Logging;
using API_Backend.Services.FileProcessing;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.MariaDB.Extensions;
using System.Reflection;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOriginWithCredentials",
        corsBuilder =>
        {
            corsBuilder.WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

// Register services
builder.Services.AddControllers();
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

// Register custom services for dependency injection
builder.Services.AddScoped<ExperimentService>();
builder.Services.AddScoped<DataVisualization>();
builder.Services.AddScoped<IDatasetService, DatasetService>();
builder.Services.AddHostedService<ExperimentWorkerService>();
builder.Services.AddSingleton<IExperimentQueue, ExperimentQueue>();
builder.Services.AddScoped<FileProcessor>();


// Register DbContext with MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 26)) // Replace with your MySQL server version
    )
);

// Register IHttpContextAccessor
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Inside Program.cs

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.MariaDB(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        tableName: "Logs",
        autoCreateTable: true
    )
    .CreateLogger();

// Replace the default logger with Serilog
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

// Enable Serilog self-logging
SelfLog.Enable(msg => File.AppendAllText("serilog-selflog.txt", msg + Environment.NewLine));

// Configure authentication
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

    // Restrict access to users with a specific email domain
    options.Events = new OAuthEvents
    {
        OnCreatingTicket = async context =>
        {
            var email = context.Identity?.FindFirst(ClaimTypes.Email)?.Value;
            var firstName = context.Identity?.FindFirst(ClaimTypes.GivenName)?.Value;
            var lastName = context.Identity?.FindFirst(ClaimTypes.Surname)?.Value;

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

            var claimsIdentity = (ClaimsIdentity)context.Principal?.Identity!;
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

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowSpecificOriginWithCredentials");

app.UseAuthentication();

// Register the middleware **after** UseAuthentication
app.UseMiddleware<SerilogUserIdEnrichmentMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();



//using API_Backend.Models;
//using API_Backend.Services.Docker_Swarm;
//using API_Backend.Models;
//using API_Backend.Services.FileProcessing;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore.Query.Internal;
//using ZstdSharp.Unsafe;

//Console.WriteLine("Starting Service");

//try
//{
//    // Setup
//    DockerSwarm experimentService;
//    //experimentService = new DockerSwarm();
//    experimentService = new DockerSwarm("10.0.0.42", "2333");
//    FileProcessor fileProcessor = new FileProcessor();

//    List<string> experimentIds = new List<string>() { "EX1" };
//    List<string> resultsPaths = new List<string>();

//    // Submit three experiments
//    foreach (string experimentId in experimentIds)
//    {
//        ExperimentResponse response = await experimentService.SubmitExperiment(new API_Backend.Models.ExperimentRequest()
//        {
//            UserID = "UA124",
//            ExperimentID = experimentId,
//            Algorithm = new API_Backend.Models.Algorithm()
//            {
//                MainClassName = "edu.wvu.rascl.driver.SupervisedMLRF",
//                JarFilePath = "Jars/supervisedmlrf_2.12-1.0.jar",
//            },
//            AlgorithmParameters = new API_Backend.Models.AlgorithmRequestParameters()
//            {
//                DatasetName = "gbt350drift_2class_labeled.csv",
//                ParameterValues = new List<API_Backend.Models.ExperimentAlgorithmParameterValue>()
//        {
//            new API_Backend.Models.ExperimentAlgorithmParameterValue() { Value = "4", AlgorithmParameter = new AlgorithmParameter() { DriverIndex = 1 } },
//            new API_Backend.Models.ExperimentAlgorithmParameterValue() { Value = "10", AlgorithmParameter = new AlgorithmParameter() { DriverIndex = 5 } },
//            new API_Backend.Models.ExperimentAlgorithmParameterValue() { Value = "gini", AlgorithmParameter = new AlgorithmParameter() { DriverIndex = 2 } },
//            new API_Backend.Models.ExperimentAlgorithmParameterValue() { Value = "32", AlgorithmParameter = new AlgorithmParameter() { DriverIndex = 4 } },
//            new API_Backend.Models.ExperimentAlgorithmParameterValue() { Value = "4", AlgorithmParameter = new AlgorithmParameter() { DriverIndex = 3 } },
//            new API_Backend.Models.ExperimentAlgorithmParameterValue() { Value = "2", AlgorithmParameter = new AlgorithmParameter() { DriverIndex = 0 } },
//        }
//            },
//            ClusterParameters = new API_Backend.Models.ClusterParameters()
//            {
//                NodeCount = 1,
//                DriverMemory = "2048m",
//                DriverCores = 1,
//                ExecutorNumber = 1,
//                ExecutorCores = 1,
//                ExecutorMemory = "2048m",
//                MemoryOverhead = 972,
//            }
//        });
//        if (response.ErrorCode != 0)
//            throw new Exception(response.ErrorMessage);
//        resultsPaths.Add(response.OutputPath);
//    }

//    // Aggregate Data for three experiments
//    Console.WriteLine("Aggregating Data");
//    fileProcessor.AggregateData("UA124", "0001", resultsPaths);
//    // fileProcessor.GetCsv("UA124", "0001", resultsPaths)

//    Console.WriteLine("Finished");

//}
//catch (Exception ex)
//{
//    Console.WriteLine($"An unknown exception has occured: {ex.Message}");
//}

