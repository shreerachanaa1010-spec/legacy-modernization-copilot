using LegacyModernization.Analyzer.Services;
using LegacyModernization.LLM.Services;
using LegacyModernization.Rag.Services;
using LegacyModernization.TestGenerator.Services;
using LegacyModernization.Verifier.Services;
using Scalar.AspNetCore;

LoadLocalEnvironment();
var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register pipeline services
builder.Services.AddSingleton<IProjectAnalyzer, ProjectAnalyzer>();
builder.Services.AddSingleton<ILlmService, GeminiService>();
builder.Services.AddSingleton<FileSystemRepositoryRetriever>();
builder.Services.AddSingleton<SymbolAwareRepositoryRetriever>();
builder.Services.AddSingleton<LongLivedPythonRepositoryRetriever>();
builder.Services.AddSingleton<HybridRepositoryRetriever>();
builder.Services.AddSingleton<IRepositoryRetriever>(serviceProvider =>
    serviceProvider.GetRequiredService<HybridRepositoryRetriever>());
builder.Services.AddSingleton<SqliteVectorStore>(_ =>
    new SqliteVectorStore(Path.Combine(AppContext.BaseDirectory, "data", "rag.db")));
builder.Services.AddSingleton<IRepositoryIndexer, SqliteRepositoryIndexer>();
builder.Services.AddSingleton<IVectorStore>(serviceProvider =>
    serviceProvider.GetRequiredService<SqliteVectorStore>());
builder.Services.AddSingleton<IAcceptedRefactoringStore>(serviceProvider =>
    serviceProvider.GetRequiredService<SqliteVectorStore>());
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

static void LoadLocalEnvironment()
{
    var candidates = new[]
    {
        Directory.GetCurrentDirectory(),
        AppContext.BaseDirectory
    };

    foreach (var start in candidates)
    {
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
        {
            var envPath = Path.Combine(directory.FullName, ".env");
            if (!File.Exists(envPath))
            {
                continue;
            }

            foreach (var line in File.ReadLines(envPath))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith('#') || !trimmed.Contains('='))
                {
                    continue;
                }

                var separator = trimmed.IndexOf('=');
                var key = trimmed[..separator].Trim();
                var value = trimmed[(separator + 1)..].Trim().Trim('"');
                if (!string.IsNullOrWhiteSpace(key) && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }

            return;
        }
    }
}
