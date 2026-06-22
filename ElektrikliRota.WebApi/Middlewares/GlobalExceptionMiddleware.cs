using System.Net;
using System.Text.Json;

namespace ElektrikliRota.WebApi.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly bool _isDevelopment;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _isDevelopment = env.IsDevelopment();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sunucuda beklenmeyen bir hata oluştu.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 500;

        object response;

        if (_isDevelopment)
        {
            // Development: Hata detaylarını göster (debug için)
            response = new
            {
                StatusCode = 500,
                Message = "Sunucu tarafında beklenmeyen bir hata oluştu.",
                Detailed = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
        else
        {
            // Production: Sadece genel mesaj döndür (güvenlik için)
            response = new
            {
                StatusCode = 500,
                Message = "Sunucu tarafında beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin."
            };
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
