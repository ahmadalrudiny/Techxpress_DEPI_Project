using System.Security.Claims;

namespace TechXpress.Web.Infrastructure;

/// <summary>
/// يسجّل أي استثناء غير متوقع مع كل التفاصيل اللازمة لمعرفة المشكلة ومكانها:
/// نوع الطلب، المسار، المستخدم، IP، TraceId، نص الاستثناء وStack Trace.
/// </summary>
public sealed class ExceptionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionLoggingMiddleware> _logger;

    public ExceptionLoggingMiddleware(RequestDelegate next, ILogger<ExceptionLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
        {
            // المستخدم أغلق الصفحة أو الطلب — ليس خطأً حقيقيًا
            throw;
        }
        catch (Exception ex)
        {
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "-";
            var userName = context.User?.Identity?.Name ?? "unknown";
            var user = context.User?.Identity?.IsAuthenticated == true
                ? $"{userName} ({userId})"
                : "زائر (غير مسجل)";

            _logger.LogError(
                ex,
                "خطأ غير متوقع | {Method} {Path}{Query} | User: {User} | IP: {Ip} | TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path.Value,
                context.Request.QueryString.Value,
                user,
                context.Connection.RemoteIpAddress?.ToString() ?? "-",
                context.TraceIdentifier);

            // نعيد رمي الاستثناء حتى يعرض ASP.NET صفحة الخطأ المناسبة
            throw;
        }
    }
}
