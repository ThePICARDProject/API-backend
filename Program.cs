using API_backend.Services.DataVisualization;
using API_backend.Services.Docker;
using API_backend.Services.FileProcessing;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<DataVisualization>();

// Configure Services
builder.Services.Configure<ExperimentOptions>(builder.Configuration.GetSection("Experiments"));
builder.Services.AddSingleton<ExperimentService>();
builder.Services.Configure<FileProcessorOptions>(builder.Configuration.GetSection("FileProcessing"));
builder.Services.AddSingleton<ExperimentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
