using iDiski.Infrastructure.Persistence;
using iDiski.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// 1. Get Connection String
// In production (Railway), use DATABASE_URL environment variable
// In development, use appsettings.json connection string
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Railway provides DATABASE_URL in postgres:// or postgresql:// format, convert to Host= format if needed
if (!string.IsNullOrEmpty(connectionString) && (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://")))
{
    // Parse postgres://user:password@host:port/database format manually
    // System.Uri doesn't recognize postgres:// as a valid scheme
    var url = connectionString.Replace("postgresql://", "").Replace("postgres://", "");

    // Split by @ to separate credentials from host
    var parts = url.Split('@');
    var credentials = parts[0].Split(':');
    var username = credentials[0];
    var password = credentials.Length > 1 ? credentials[1] : "";

    // Split host part by / to separate host:port from database
    var hostParts = parts[1].Split('/');
    var hostAndPort = hostParts[0].Split(':');
    var host = hostAndPort[0];
    var port = hostAndPort.Length > 1 ? hostAndPort[1] : "5432";
    var database = hostParts.Length > 1 ? hostParts[1] : "";

    connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
}

Console.WriteLine($"Using connection string: {connectionString?.Substring(0, Math.Min(50, connectionString?.Length ?? 0))}...");

// 2. Register DbContext in the DI Container
builder.Services.AddDbContext<LeagueDbContext>(options =>
{
    options.UseNpgsql(connectionString,
        // Tell EF to look for migrations in the Infrastructure project
        b => b.MigrationsAssembly("iDiski.Infrastructure"));

    // Suppress pending model changes warning during migration
    options.ConfigureWarnings(warnings =>
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// 3. Register the Interface for the Application Layer
builder.Services.AddScoped<ILeagueDbContext>(provider =>
    provider.GetRequiredService<LeagueDbContext>());

// 4. Register MediatR (CQRS pattern)
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(iDiski.Application.Teams.TeamDto).Assembly));

// 5. Register FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(iDiski.Application.Teams.TeamDto).Assembly);

// 5.5. Register File Storage Service
builder.Services.AddScoped<iDiski.Application.Common.Interfaces.IFileStorageService,
    iDiski.Infrastructure.Services.LocalFileStorageService>();

// 6. Configure CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // Development URLs
        var origins = new List<string>
        {
            "http://localhost:4200",
            "https://localhost:4200"
        };

        // Production URLs (add your actual Vercel URL after deployment)
        var productionOrigin = builder.Configuration["ProductionOrigin"];
        if (!string.IsNullOrEmpty(productionOrigin))
        {
            origins.Add(productionOrigin);
        }

        policy.WithOrigins(origins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Senior Tip: Add Swagger UI if you want to test the endpoints visually
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "v1"));
}

app.UseCors(); // Enable CORS
app.UseStaticFiles(); // Serve static files from wwwroot (uploaded images)
app.UseHttpsRedirection();
app.MapControllers(); // This maps your ArticlesController, TeamsController, etc.

// Health check endpoint for Railway
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithTags("Health");

// Migration endpoint - run this first on Railway to create database schema
app.MapPost("/api/migrate", async (LeagueDbContext db) =>
{
    try
    {
        await db.Database.MigrateAsync();
        return Results.Ok(new { message = "Database migrations applied successfully!" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Migration failed: {ex.Message}");
    }
}).WithTags("Database");

// ── SEED DATA ENDPOINTS ───────────────────────────────────────────────────────
// Call these endpoints to seed the database with sample data

// Basic seed (current season data)
// Example: GET http://localhost:5207/api/seed
app.MapGet("/api/seed", async (LeagueDbContext db, IServiceProvider services) =>
{
    try
    {
        await iDiski.Infrastructure.Seed.SeedData.Initialize(services);
        return Results.Ok(new { message = "Database seeded successfully!" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to seed database: {ex.Message}");
    }
}).WithTags("Seed");

// Historical seed (PSL 2015/16, Premier League 2012/13, WPL 2019/20)
// Example: GET http://localhost:5207/api/seed/historical
app.MapGet("/api/seed/historical", async (LeagueDbContext db) =>
{
    try
    {
        await iDiski.Infrastructure.Seed.HistoricalDataSeeder.SeedHistoricalData(db);
        return Results.Ok(new { message = "Historical data seeded successfully! Added PSL 2015/16, Premier League 2012/13, and WPL 2019/20 seasons." });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to seed historical data: {ex.Message}");
    }
}).WithTags("Seed");

// Comprehensive historical seed with REAL players, results, and match events
// Example: GET http://localhost:5207/api/seed/comprehensive
app.MapGet("/api/seed/comprehensive", async (LeagueDbContext db) =>
{
    try
    {
        await iDiski.Infrastructure.Seed.ComprehensiveHistoricalSeeder.SeedComprehensiveData(db);
        return Results.Ok(new {
            message = "Comprehensive historical data seeded successfully!",
            details = new {
                psl_2015_16 = "✓ Real players (Billiat, Kekana, Castro, etc.) with match events",
                epl_2012_13 = "✓ Manchester Derby and iconic matches (Van Persie, Rooney, Agüero)",
                ligaf_2019_20 = "✓ Barcelona Femení dominance (Hermoso, Putellas, Martens)",
                articles = "✓ 4 articles with YouTube interview/highlight references"
            }
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to seed comprehensive data: {ex.Message}");
    }
}).WithTags("Seed");

// Clear all data and reseed comprehensive
// WARNING: This deletes ALL data! Example: DELETE http://localhost:5207/api/seed/reset
app.MapDelete("/api/seed/reset", async (LeagueDbContext db) =>
{
    try
    {
        // Delete all data in correct order (respecting foreign keys)
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"MatchEvents\" CASCADE");
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Suspensions\" CASCADE");
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"MatchResults\" CASCADE");
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Players\" CASCADE");
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Articles\" CASCADE");
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Teams\" CASCADE");
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Divisions\" CASCADE");
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Sponsors\" CASCADE");
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"PageLayoutConfigs\" CASCADE");

        // Reseed comprehensive data
        await iDiski.Infrastructure.Seed.ComprehensiveHistoricalSeeder.SeedComprehensiveData(db);

        return Results.Ok(new { message = "Database cleared and reseeded with comprehensive data!" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to reset and reseed: {ex.Message}");
    }
}).WithTags("Seed");

app.Run();