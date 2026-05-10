using System.Text;
using ConsultancyManagement.Infrastructure;
using ConsultancyManagement.Infrastructure.Data;
using ConsultancyManagement.Infrastructure.Seed;
using ConsultancyManagement.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.Configure(options =>
{
    options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId;
});

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 21 * 1024 * 1024;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestMethod | HttpLoggingFields.RequestPath |
                            HttpLoggingFields.ResponseStatusCode | HttpLoggingFields.Duration;
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Consultancy Management System API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
    c.MapType<IFormFile>(() => new OpenApiSchema { Type = "string", Format = "binary" });
    c.OperationFilter<ConsultancyManagement.Api.Swagger.FileUploadOperationFilter>();
});

builder.Services.AddCors(options =>
{
    var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    var origins = (configuredOrigins is { Length: > 0 } ? configuredOrigins : new[] { "http://localhost:4200" })
        .Where(o => !string.IsNullOrWhiteSpace(o))
        .Select(o => o.Trim().TrimEnd('/'))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    options.AddPolicy("Angular", policy =>
    {
        if (origins.Length > 0)
        {
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
if (builder.Environment.IsProduction())
{
    if (jwtKey.Length < 32)
        throw new InvalidOperationException("Production requires Jwt:Key at least 32 characters (set Jwt__Key env var or secrets).");
    if (jwtKey.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Production cannot use placeholder Jwt:Key. Set Jwt__Key environment variable.");
}

builder.Services.AddAuthentication(options =>
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
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("Bootstrap")
    .LogInformation("Hosting environment: {Environment}", app.Environment.EnvironmentName);

if (app.Environment.IsProduction())
{
    var corsOrigins = app.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    var corsEffective = corsOrigins
        .Where(o => !string.IsNullOrWhiteSpace(o))
        .Select(o => o.Trim().TrimEnd('/'))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
    if (corsEffective.Length == 0)
    {
        throw new InvalidOperationException(
            "Production requires Cors:AllowedOrigins. Set environment variables, e.g. Cors__AllowedOrigins__0=https://your-spa-domain.com");
    }
}

var enableSwagger = app.Environment.IsDevelopment()
                    || app.Configuration.GetValue("EnableSwagger", false);

var wwwroot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(wwwroot, "uploads"));

app.UseForwardedHeaders();
app.UseHttpLogging();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS must run before auth and before HTTPS redirection so preflight (OPTIONS) and API calls get Access-Control-* headers.
app.UseCors("Angular");
// Behind nginx/docker TLS termination: HTTPS is handled by the proxy; Kestrel sees HTTP.
// Set DISABLE_HTTPS_REDIRECTION=true or DisableHttpsRedirection in config to avoid redirect loops.
var disableHttpsRedirect = app.Configuration.GetValue("DisableHttpsRedirection", false)
    || string.Equals(Environment.GetEnvironmentVariable("DISABLE_HTTPS_REDIRECTION"), "true",
        StringComparison.OrdinalIgnoreCase);
if (!app.Environment.IsDevelopment() && !disableHttpsRedirect)
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Redirect(enableSwagger ? "/swagger" : "/health"));

app.MapGet("/health", async (ApplicationDbContext db, CancellationToken cancellationToken) =>
{
    var ok = await db.Database.CanConnectAsync(cancellationToken);
    return ok
        ? Results.Ok(new
        {
            status = "ok",
            service = "consultancy-api",
            database = "up",
            utc = DateTime.UtcNow
        })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
}).AllowAnonymous();

var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
var startupLogger = loggerFactory.CreateLogger("Startup");
try
{
    await DbSeeder.SeedAsync(app.Services, startupLogger, app.Environment);
}
catch (Exception ex)
{
    startupLogger.LogError(ex, "Database seeding failed. Ensure PostgreSQL is running and connection string is valid.");
    if (!app.Environment.IsDevelopment())
        throw;
}

app.Run();
