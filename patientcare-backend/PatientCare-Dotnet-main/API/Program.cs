using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using API.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;
using PatientCare.Core.Common;
using PatientCare.Core.Interfaces;
using PatientCare.Infrastructure.Data;
using PatientCare.Infrastructure.Repositories;
using PatientCare.Service.Mapping;
using PatientCare.Service.Services;

// Npgsql: DateTime נשמר כ-timestamp without time zone ומקבל כל Kind (כמו שהיה ב-SQL Server).
// חייב להיקבע לפני כל שימוש ב-EF, גם בזמן עיצוב (dotnet ef).
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Database
    // PostgreSQL. ב-Render מגיעה כתובת מהצורה postgres://user:pass@host:5432/db, והיא מומרת למחרוזת של Npgsql.
    var connectionString = ConnectionStringHelper.ToNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? builder.Configuration["DATABASE_URL"]
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection (or DATABASE_URL) is not configured."));

    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

    // CORS: כתובות האתר (frontend) שמותר להן לקרוא ל-API, מופרדות בפסיק. למשל https://patientcare-web.onrender.com
    var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("X-Correlation-Id");
            }
        }));

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // JWT settings. המפתח הסודי לא נשמר בקוד: הוא מגיע מ-User Secrets (או ממשתנה סביבה Jwt__Key).
    var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
        ?? throw new InvalidOperationException("Jwt configuration section is missing.");

    if (string.IsNullOrWhiteSpace(jwtSettings.Key))
    {
        if (!builder.Environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Jwt:Key is not configured. Set it with: dotnet user-secrets set \"Jwt:Key\" \"<long random value>\" " +
                "or with the environment variable Jwt__Key.");
        }

        // בפיתוח בלבד: מפתח זמני אקראי כדי שהמערכת תעבוד בלי הגדרה. אחרי הפעלה מחדש צריך להתחבר שוב.
        jwtSettings.Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        logger.Warn("Jwt:Key is not configured. Using a temporary random key for this run (set it with user-secrets).");
    }
    else if (Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32)
    {
        throw new InvalidOperationException("Jwt:Key must be at least 32 bytes long.");
    }

    builder.Services.Configure<JwtSettings>(options =>
    {
        options.Key = jwtSettings.Key;
        options.Issuer = jwtSettings.Issuer;
        options.Audience = jwtSettings.Audience;
        options.ExpiryMinutes = jwtSettings.ExpiryMinutes;
    });

    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                ClockSkew = TimeSpan.FromMinutes(1),
                RoleClaimType = ClaimTypes.Role
            };
        });

    builder.Services.AddAuthorization();

    // Repositories + Unit of Work (Scoped: מופע אחד לכל בקשה, באותו DbContext)
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<ICaregiverRepository, CaregiverRepository>();
    builder.Services.AddScoped<IPatientRepository, PatientRepository>();
    builder.Services.AddScoped<ICareShiftRepository, CareShiftRepository>();
    builder.Services.AddScoped<ISwapRequestRepository, SwapRequestRepository>();

    // Application services
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IPatientService, PatientService>();
    builder.Services.AddScoped<ICareShiftService, CareShiftService>();
    builder.Services.AddScoped<ISwapRequestService, SwapRequestService>();
    builder.Services.AddScoped<IAdminService, AdminService>();

    // AutoMapper (Singleton: ההגדרות נבנות פעם אחת)
    builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

    builder.Services
        .AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            // שגיאות ולידציה בפורמט אחיד: { message, errors } כדי שהלקוח יציג את ההודעה הראשונה.
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(e => e.Value is { Errors.Count: > 0 })
                    .ToDictionary(
                        e => e.Key,
                        e => e.Value!.Errors.Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "ערך לא תקין." : x.ErrorMessage).ToArray());

                var message = errors.Values.SelectMany(v => v).FirstOrDefault() ?? "הבקשה אינה תקינה.";
                return new BadRequestObjectResult(new { message, errors });
            };
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "PatientCare API", Version = "v1" });

        var bearerScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter a valid JWT token.",
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        };

        options.AddSecurityDefinition("Bearer", bearerScheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { bearerScheme, Array.Empty<string>() }
        });
    });

    var app = builder.Build();
    app.Logger.LogInformation(
    "CORS allowed origins: {AllowedOrigins}",
    allowedOrigins.Length > 0 ? string.Join(", ", allowedOrigins) : "(none)");

    // מחיל מיגרציות ממתינות בעליית השרת (גם ב-Render). נתוני דמו נזרעים רק אם הוגדרה סיסמת דמו.
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            if (db.Database.GetMigrations().Any())
            {
                await db.Database.MigrateAsync();
            }
            else
            {
                // עוד לא נוצרה מיגרציה ל-PostgreSQL (ראו README). בינתיים הטבלאות נוצרות ישירות מהמודל.
                startupLogger.LogWarning("No migrations found. Creating the schema from the model. Run: dotnet ef migrations add InitialCreate");
                await db.Database.EnsureCreatedAsync();
            }
            await DbSeeder.SeedAsync(
                db,
                app.Configuration["Seed:DemoPassword"],
                app.Configuration["Seed:AdminEmail"],
                startupLogger,
                BCrypt.Net.BCrypt.HashPassword);
        }
        catch (Exception ex)
        {
            startupLogger.LogError(ex, "Database migration or seeding failed.");
            if (!app.Environment.IsDevelopment())
            {
                throw;
            }
        }
    }

    // מאחורי ה-proxy של Render: כך הבקשה מזוהה כ-HTTPS והכתובת המקורית נשמרת.
    app.UseForwardedHeaders();

    // סדר ה-Middleware: CorrelationId (כדי שכל לוג יישא מזהה) → לוג בקשות → טיפול בחריגות → שאר הצינור.
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // ב-Render ה-HTTPS מסתיים ב-proxy, ולכן ההפניה נעשית רק בפיתוח המקומי.
    if (app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseCors();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Application stopped because of an exception.");
    throw;
}
finally
{
    LogManager.Shutdown();
}

/// <summary>ממיר כתובת postgres://... (כמו ש-Render נותן) למחרוזת התחברות של Npgsql.</summary>
public static class ConnectionStringHelper
{
    public static string ToNpgsql(string value)
    {
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);
        var port = uri.Port > 0 ? uri.Port : 5432;

        return $"Host={uri.Host};Port={port};Database={uri.AbsolutePath.TrimStart('/')};" +
               $"Username={Uri.UnescapeDataString(userInfo[0])};" +
               $"Password={Uri.UnescapeDataString(userInfo.Length > 1 ? userInfo[1] : string.Empty)};" +
               "SSL Mode=Require;Trust Server Certificate=true";
    }
}
