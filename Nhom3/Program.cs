using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Nhom3.Application.Services;
using Nhom3.Domain.Interfaces;
using Nhom3.Infrastructure.Data;
using Nhom3.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

var connectionString = "Data Source=nhom3.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString)
);

builder.Services.AddScoped<IUser, UserRepo>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenBlacklist, TokenBlacklistRepo>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
       policy.WithOrigins("http://192.168.31.118:5173", "http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhap 'Bearer {token}'"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Jwt:Key is missing");

if (string.IsNullOrWhiteSpace(jwtIssuer))
    throw new InvalidOperationException("Jwt:Issuer is missing");

if (string.IsNullOrWhiteSpace(jwtAudience))
    throw new InvalidOperationException("Jwt:Audience is missing");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                var tokenType = context.Principal?.FindFirst("typ")?.Value;

                if (string.IsNullOrWhiteSpace(jti) || tokenType != "access")
                {
                    context.Fail("Invalid token");
                    return;
                }

                var blacklist = context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklist>();
                if (await blacklist.IsBlacklistedAsync(jti))
                    context.Fail("Token revoked");
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.Use(async (context, next) =>
{
    if (HttpMethods.IsGet(context.Request.Method))
    {
        var path = context.Request.Path;

        var isList = path.Equals("/api/User", StringComparison.OrdinalIgnoreCase);
        var isById = path.StartsWithSegments("/api/User", out var remaining)
                     && int.TryParse(remaining.Value.Trim('/'), out _);

        if (isList || isById)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var allowed = new HashSet<string>
            {
                "127.0.0.1",
                "::1",
                "192.168.1.10" // IP máy chạy Order service
            };

            if (ip == null || !allowed.Contains(ip))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Forbidden");
                return;
            }
        }
    }

    await next();
});
app.UseAuthentication();
app.UseAuthorization();

// app.UseHttpsRedirection();

app.MapControllers();

app.Run();

