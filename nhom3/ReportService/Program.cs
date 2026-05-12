using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ReportService.Data;
using ReportService.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── Database riêng cho Report Service ────────────────────────────────────────
builder.Services.AddDbContext<ReportDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ReportDB")));

// ─── JWT Validation — chỉ validate, KHÔNG cấp token ─────────────────────────
// Dùng cùng Key với UserAuthService (shared secret)
var jwtKey    = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = false,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
});

// ─── HttpClient gọi sang Order Service và Product Service qua Gateway ────────
builder.Services.AddHttpClient("gateway", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Services:GatewayUrl"]!);
    c.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ─── DI ───────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IReportService, ReportAggregatorService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Report Service - Nhóm 3",
        Version     = "v1",
        Description = "Báo cáo doanh thu, thống kê tổng hợp — chỉ Admin"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization", Type = SecuritySchemeType.Http,
        Scheme       = "Bearer",        BearerFormat = "JWT",
        In           = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Report Service v1"));

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReportDbContext>();
    db.Database.Migrate();
}

app.Run();
