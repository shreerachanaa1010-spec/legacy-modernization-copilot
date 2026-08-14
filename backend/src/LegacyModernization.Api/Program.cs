using LegacyModernization.Analyzer.Services;
using LegacyModernization.LLM.Services;
using LegacyModernization.TestGenerator.Services;
using LegacyModernization.Verifier.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register pipeline services
builder.Services.AddSingleton<IProjectAnalyzer, ProjectAnalyzer>();
builder.Services.AddSingleton<ILlmService, GeminiService>();
builder.Services.AddSingleton<ITestGenerator, GeminiTestGenerator>();
builder.Services.AddSingleton<VerificationService>();
builder.Services.AddSingleton<TestRunner>();

// CORS — allow React dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// OpenAPI + Scalar interactive UI
app.MapOpenApi();
app.MapScalarApiReference();

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
