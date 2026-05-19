using System;
using Microsoft.EntityFrameworkCore;
using nhom2.Infrastructure.Data;
using nhom2.Application.Services;
using nhom2.Domain.Interfaces;
using nhom2.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5000", "https://0.0.0.0:5001");


// Đăng ký DbContext với SQLite
var connectionString = "Data Source=nhom2.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString)
);

// Đăng ký Dependency Injection
// Repository
builder.Services.AddScoped<IOrder, OrderRepo>();

// Service
builder.Services.AddScoped<IOrderService, OrderService>();

// Thêm Controllers - chỉ định rõ Assembly
builder.Services.AddControllers()
    .AddApplicationPart(typeof(nhom2.Api.Order.OrderController).Assembly)
    .AddApplicationPart(typeof(nhom2.Api.Mock.MockUserController).Assembly)
    .AddApplicationPart(typeof(nhom2.Api.Mock.MockProductController).Assembly);

// Thêm Swagger/OpenAPI (tùy chọn, để test API dễ hơn)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Thêm CORS (frontend có thể gọi API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://192.168.1.100:5173")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// BUILD APP
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Áp dụng migration 
    dbContext.Database.Migrate();
  
    Console.WriteLine("✅ Database initialized successfully!");
}

// Swagger UI - LUÔN BẬT để test API dễ hơn
app.UseSwagger();
app.UseSwaggerUI();

// HTTPS redirect
app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowFrontend");

// Authentication/Authorization 
app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// run app
Console.WriteLine("Server starting on https://0.0.0.0:5001 and http://0.0.0.0:5000");
app.Run();
