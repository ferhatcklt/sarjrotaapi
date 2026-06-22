using ElektrikliRota.Application.Services;
using ElektrikliRota.Core.Interfaces;
using ElektrikliRota.Infrastructure.Data;
using ElektrikliRota.Infrastructure.Services;
using ElektrikliRota.WebApi.Middlewares;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Rate Limiting Ayarları (DoS Koruması)
builder.Services.AddRateLimiter(options =>
{
    // Route hesaplama endpoint'i için sıkı limit (IP başına dakikada 10 istek)
    options.AddFixedWindowLimiter("RouteApiLimit", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });

    // Genel API limiti (IP başına dakikada 60 istek)
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Configure Database (SQLite for quick setup)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=elektriklirota.db"));

// Configure HttpClientFactory
builder.Services.AddHttpClient();

// Configure Repositories
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IStationRepository, StationRepository>();

// Configure Services
builder.Services.AddScoped<IRouteService, RouteService>();

// Configure AppServices
builder.Services.AddScoped<VehicleAppService>();
builder.Services.AddScoped<StationAppService>();
builder.Services.AddScoped<RouteAppService>();

// CORS - Sıkılaştırılmış Politika
builder.Services.AddCors(options =>
{
    options.AddPolicy("StrictCors", builder =>
    {
        builder.WithOrigins(
            "http://localhost:5173", // Web Vite
            "http://localhost:3000", // Alternatif Web
            "http://10.0.2.2:8081",  // Android Emulator Expo
            "http://localhost:8081",  // iOS Simulator Expo
            "http://192.168.1.108:8081", // Fiziksel cihaz Expo
            "https://sarjrota.com.tr",
            "https://www.sarjrota.com.tr",
            "https://sarjrota-api.fcstudios.workers.dev" // Cloudflare Worker Proxy
        )
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

var app = builder.Build();

// Custom Güvenlik ve Hata Yakalama Middleware'leri
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

// CORS — ApiKeyMiddleware'den ÖNCE çalışmalı (403 yanıtlarında da CORS header'ları olsun)
app.UseCors("StrictCors");

app.UseMiddleware<ApiKeyMiddleware>();

// Rate Limiting'i devreye al
app.UseRateLimiter();

// Ensure Database is Created and Seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
    
    // Yolu Data klasörüne göre ayarlıyoruz
    var dataPath = Path.Combine(app.Environment.ContentRootPath, "..", "ElektrikliRota.Infrastructure", "Data");
    DbInitializer.Initialize(context, dataPath);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Ana dizini swagger'a yönlendir
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

app.Run();
