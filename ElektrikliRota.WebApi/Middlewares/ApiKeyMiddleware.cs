using System.Security.Cryptography;
using System.Text;

namespace ElektrikliRota.WebApi.Middlewares;

/// <summary>
/// Tüm API isteklerinde X-Api-Key header kontrolü yapar.
/// Tarayıcıdan direkt URL açma girişimlerini engeller.
/// Swagger (Development) ortamında devre dışıdır.
/// </summary>
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _apiKey;
    private readonly bool _isDevelopment;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, IWebHostEnvironment env, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _isDevelopment = env.IsDevelopment();
        _logger = logger;

        var apiKey = Environment.GetEnvironmentVariable("SARJROTA_API_KEY") 
                     ?? configuration["ApiSettings:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            if (_isDevelopment)
            {
                apiKey = "dev-only-key";
            }
            else
            {
                throw new InvalidOperationException(
                    "Production ortamında API Key tanımlanmalıdır! " +
                    "SARJROTA_API_KEY environment variable veya ApiSettings:ApiKey config değerini ayarlayın.");
            }
        }

        _apiKey = apiKey;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Swagger ve health-check yollarını atla (Development)
        if (_isDevelopment && (path.StartsWith("/swagger") || path == "/"))
        {
            await _next(context);
            return;
        }

        // Root redirect'i her zaman izin ver
        if (path == "/")
        {
            await _next(context);
            return;
        }

        // CORS preflight (OPTIONS) isteklerine izin ver
        if (context.Request.Method == "OPTIONS")
        {
            await _next(context);
            return;
        }

        // API Key'i sadece Header'dan al (Query string güvenlik riski — loglanabilir)
        var extractedApiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(extractedApiKey) || !TimingSafeEqual(extractedApiKey, _apiKey))
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "bilinmiyor";
            var method = context.Request.Method;
            var userAgent = context.Request.Headers.UserAgent.FirstOrDefault() ?? "yok";

            if (string.IsNullOrEmpty(extractedApiKey))
            {
                _logger.LogWarning(
                    "🚫 API Key eksik — IP: {IP} | {Method} {Path} | User-Agent: {UserAgent}",
                    ip, method, path, userAgent);
            }
            else
            {
                _logger.LogWarning(
                    "🚫 Geçersiz API Key denemesi — IP: {IP} | {Method} {Path} | User-Agent: {UserAgent}",
                    ip, method, path, userAgent);
            }

            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"message\":\"Erişim reddedildi. Geçersiz veya eksik API anahtarı.\"}");
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Timing attack'a karşı sabit zamanlı string karşılaştırması.
    /// </summary>
    private static bool TimingSafeEqual(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
