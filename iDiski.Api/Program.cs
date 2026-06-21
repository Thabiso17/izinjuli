using iDiski.Infrastructure.Persistence;
using iDiski.Application.Common.Interfaces;
using iDiski.Infrastructure.Services;
using iDiski.Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using FluentValidation;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Get Connection String
// In production (Railway), use DATABASE_URL environment variable
// In development, use appsettings.json connection string
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Railway provides DATABASE_URL in postgres:// or postgresql:// format, convert to Host= format if needed
if (!string.IsNullOrEmpty(connectionString) && (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://")))
{
    var url = connectionString.Replace("postgresql://", "postgres://");
    var uri = new Uri(url);

    var username = uri.UserInfo.Split(':')[0];
    var password = uri.UserInfo.Contains(':') ? uri.UserInfo.Split(':')[1] : "";
    var host = uri.Host;
    var port = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/');

    connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
}

Console.WriteLine("Database connection configured.");

// 2. Register DbContext in the DI Container
builder.Services.AddDbContext<LeagueDbContext>(options =>
{
    options.UseNpgsql(connectionString,
        b =>
        {
            b.MigrationsAssembly("iDiski.Infrastructure");
            b.CommandTimeout(60); // 60 seconds for complex queries
        });

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

// 5.4. Register Authentication Services
builder.Services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddHttpContextAccessor();

// 5.4.1 Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey is not configured");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is not configured");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is not configured");

var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero // No tolerance for token expiry
        };

        // Extract claims from JWT and set HttpContext.User
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var userId = context.Principal?.FindFirst("userId")?.Value;
                var email = context.Principal?.FindFirst("email")?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine($"✅ JWT validated for user: {email} ({userId})");
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsJsonAsync(new { error = "Token has expired" });
                }

                Console.WriteLine($"⚠️ Authentication failed: {context.Exception?.Message}");
                return Task.CompletedTask;
            }
        };
    });

// 5.6. Register Authorization Handlers
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", policy =>
        policy.RequireRole(Role.SuperAdmin.ToString()));

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole(
            Role.SuperAdmin.ToString(),
            Role.DivisionAdmin.ToString(),
            Role.TeamAdmin.ToString()));

    options.AddPolicy("CanManageTeams", policy =>
        policy.RequireRole(
            Role.SuperAdmin.ToString(),
            Role.TeamAdmin.ToString()));

    options.AddPolicy("CanManageDivisions", policy =>
        policy.RequireRole(
            Role.SuperAdmin.ToString(),
            Role.DivisionAdmin.ToString()));
});

builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
    iDiski.Infrastructure.Authorization.TeamOwnershipHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
    iDiski.Infrastructure.Authorization.DivisionOwnershipHandler>();

// 5.7. Register File Storage Service
// Use Cloudinary in production (Railway), local storage in development
var useCloudinary = Environment.GetEnvironmentVariable("USE_CLOUDINARY") == "true"
    || !builder.Environment.IsDevelopment();

if (useCloudinary)
{
    Console.WriteLine("Using Cloudinary for file storage");
    builder.Services.AddScoped<iDiski.Application.Common.Interfaces.IFileStorageService,
        iDiski.Infrastructure.Services.CloudinaryFileStorageService>();
}
else
{
    Console.WriteLine("Using local file storage (wwwroot/uploads)");
    builder.Services.AddScoped<iDiski.Application.Common.Interfaces.IFileStorageService,
        iDiski.Infrastructure.Services.LocalFileStorageService>();
}

// 6. Configure CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // Development URLs
        var origins = new List<string>
        {
            "http://localhost:4200",
            "https://localhost:4200",
            "https://izinjuli.vercel.app", // Production Vercel URL
            "https://idiski-api.up.railway.app" // Railway API URL (for testing)
        };

        // Production URLs (add your actual Vercel URL after deployment)
        var productionOrigin = builder.Configuration["ProductionOrigin"];
        if (!string.IsNullOrEmpty(productionOrigin))
        {
            origins.Add(productionOrigin);
        }

        policy.WithOrigins(origins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();

var app = builder.Build();

// Auto-migrate database on startup
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<LeagueDbContext>();
        await db.Database.MigrateAsync();
        Console.WriteLine("✅ Database migration applied successfully on startup");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Database migration failed on startup: {ex.Message}");
    throw;
}

// Auto-seed test authentication users on startup (development only)
try
{
    await iDiski.Infrastructure.Seed.AuthTestDataSeeder.SeedAuthTestUsers(app.Services);
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Auth test data seeding failed: {ex.Message}");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Senior Tip: Add Swagger UI if you want to test the endpoints visually
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "v1"));
}

app.UseMiddleware<iDiski.Api.Middleware.ExceptionHandlingMiddleware>();
app.UseStaticFiles(); // Serve static files from wwwroot (uploaded images)
app.UseHttpsRedirection();

// CORS must be applied BEFORE authentication
app.UseCors();

// Add JWT Authentication Middleware
app.UseAuthentication(); // Validates JWT tokens
app.UseAuthorization();  // Checks [Authorize] attributes

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

// Manual SQL execution endpoint - for applying migrations when EF Core fails
app.MapPost("/api/migrate/manual", async (LeagueDbContext db) =>
{
    try
    {
        // Add IsPinned column to Articles if it doesn't exist
        await db.Database.ExecuteSqlRawAsync(@"
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                               WHERE table_name='Articles' AND column_name='IsPinned') THEN
                    ALTER TABLE ""Articles"" ADD COLUMN ""IsPinned"" boolean NOT NULL DEFAULT false;
                    CREATE INDEX ""IX_Articles_IsPinned"" ON ""Articles"" (""IsPinned"");
                END IF;
            END $$;
        ");

        // Create Videos table if it doesn't exist
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""Videos"" (
                ""Id"" uuid NOT NULL,
                ""Title"" character varying(300) NOT NULL,
                ""VideoUrl"" character varying(500) NOT NULL,
                ""Description"" text,
                ""ThumbnailUrl"" text,
                ""Author"" character varying(100) NOT NULL,
                ""IsPublished"" boolean NOT NULL,
                ""PublishedAt"" timestamp with time zone,
                ""IsPinned"" boolean NOT NULL,
                ""ViewCount"" integer NOT NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""UpdatedAt"" timestamp with time zone NOT NULL,
                CONSTRAINT ""PK_Videos"" PRIMARY KEY (""Id"")
            );

            CREATE INDEX IF NOT EXISTS ""IX_Videos_IsPinned"" ON ""Videos"" (""IsPinned"");
            CREATE INDEX IF NOT EXISTS ""IX_Videos_PublishedAt"" ON ""Videos"" (""PublishedAt"");
        ");

        // Update migration history
        await db.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
            VALUES ('20260523160000_AddIsPinnedToArticle', '9.0.0')
            ON CONFLICT DO NOTHING;

            INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
            VALUES ('20260523160100_AddVideoEntity', '9.0.0')
            ON CONFLICT DO NOTHING;
        ");

        return Results.Ok(new { message = "Manual migrations applied successfully!" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Manual migration failed: {ex.Message}");
    }
}).WithTags("Database");

// Fix UpdatedAt column nullability in Videos table
app.MapPost("/api/migrate/fix-videos", async (LeagueDbContext db) =>
{
    try
    {
        await db.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE ""Videos"" ALTER COLUMN ""UpdatedAt"" DROP NOT NULL;
        ");

        return Results.Ok(new { message = "Videos table UpdatedAt column fixed successfully!" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Fix failed: {ex.Message}");
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