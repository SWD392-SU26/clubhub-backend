using System.Text;
using ClubHub.API.Data;
using ClubHub.API.Middlewares;
using ClubHub.API.Repositories;
using ClubHub.API.Services.Implementations;
using ClubHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using DotNetEnv;

// Load environment variables from .env file
Env.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection"),
    sqlOptions => sqlOptions.EnableRetryOnFailure(3)));

// ── Authentication / JWT ──────────────────────────────────────────────────────
var jwtConfig = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtConfig["Key"]!);

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
        ValidIssuer = jwtConfig["Issuer"],
        ValidAudience = jwtConfig["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader))
            {
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    context.Token = authHeader["Bearer ".Length..].Trim();
                }
                else
                {
                    context.Token = authHeader;
                }
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.WithOrigins("https://clubhub-project.vercel.app")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

// ── Repositories & Unit of Work ───────────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IClubService, ClubService>();
builder.Services.AddScoped<IMembershipService, MembershipService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IPointService, PointService>();
builder.Services.AddScoped<IProposalService, ProposalService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// ── Email Service (SMTP for OTP) ───────────────────────────────────────────────
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// ── User Management Service ────────────────────────────────────────────────────
builder.Services.AddScoped<IUserManagementService, UserManagementService>();

// ── AWS S3 Storage Service ─────────────────────────────────────────────────────
builder.Services.AddScoped<IStorageService, AwsS3StorageService>();

// ── Internal Club Activity Services ────────────────────────────────────────────
builder.Services.AddScoped<IActivityService, ActivityService>();

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers(opts =>
{
    // Allow large file uploads
    opts.MaxIAsyncEnumerableBufferLimit = 10 * 1024 * 1024;
})
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ClubHub API",
        Description = "Hệ thống quản lý câu lạc bộ sinh viên"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Dán token trực tiếp (có hoặc không kèm 'Bearer ' prefix)"
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

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ClubHub API v1");
        c.RoutePrefix = string.Empty; // Swagger at root
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Auto-migrate & Seed on startup ────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    // ── Seed Admin Account ────────────────────────────────────────────────────
    const string adminEmail = "admin@gmail.com";
    if (!db.Users.Any(u => u.Email == adminEmail))
    {
        var admin = new ClubHub.API.Entities.User
        {
            Id           = Guid.NewGuid(),
            FullName     = "Administrator",
            Username     = "admin",
            Email        = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("12345"),
            Role         = ClubHub.API.Enums.Role.UniversityAdmin,
            Status       = ClubHub.API.Enums.UserStatus.Active,
            IsEmailVerified = true,
            CreatedAt    = DateTime.UtcNow
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync();
        Console.WriteLine("[Seed] Admin account created: admin@gmail.com");
    }
    else
    {
        Console.WriteLine("[Seed] Admin account already exists, skipping.");
    }
}

app.Run();
