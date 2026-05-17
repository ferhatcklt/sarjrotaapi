namespace ElektrikliRota.WebApi.Middlewares;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Temel HTTP Güvenlik Başlıkları
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY"); // Tıklama hırsızlığını (Clickjacking) önler
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block"); // Tarayıcı bazlı XSS filtresi
        
        // Strict-Transport-Security (Sadece HTTPS üzerinden çalışması için, prod'da aktifleştirilebilir)
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");

        // Content Security Policy (Sadece API olduğu için dış kaynak çalıştırılmasına gerek yok)
        context.Response.Headers.Append("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none';");

        await _next(context);
    }
}
