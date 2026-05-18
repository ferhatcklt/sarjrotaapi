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

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, IWebHostEnvironment env)
    {
        _next = next;
        _apiKey = Environment.GetEnvironmentVariable("SARJROTA_API_KEY") 
                  ?? configuration["ApiSettings:ApiKey"] 
                  ?? "dev-only-key";
        _isDevelopment = env.IsDevelopment();
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

        // API Key'i Header'dan veya Query String'den al
        var extractedApiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault() 
                              ?? context.Request.Query["key"].FirstOrDefault();

        if (string.IsNullOrEmpty(extractedApiKey) || extractedApiKey != _apiKey)
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"message\":\"Erişim reddedildi. Geçersiz veya eksik API anahtarı.\"}");
            return;
        }

        await _next(context);
    }
}
