using AiComplaintAssistant.Api.Extensions;
using AiComplaintAssistant.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Config
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Logging setup
builder.Logging.ClearProviders(); // Remove default providers
builder.Logging.AddConsole(); // Log to console
builder.Logging.SetMinimumLevel(LogLevel.Debug); // Log everything from Debug and above

// Services
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.RegisterSemanticKernel(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<AIService>();

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapApi();

app.Run();
