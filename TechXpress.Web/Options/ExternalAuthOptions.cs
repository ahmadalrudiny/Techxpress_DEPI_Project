namespace TechXpress.Web.Options;

/// <summary>
/// يوضّح للـ Views مزوّدي تسجيل الدخول الخارجيين المفعّلين.
/// المزوّد يُفعّل فقط عند إضافة بيانات اعتماده في إعدادات التطبيق (User Secrets / Azure App Settings).
/// </summary>
public class ExternalAuthOptions
{
    /// <summary>True when Google client id/secret are configured.</summary>
    public bool GoogleEnabled { get; init; }

    /// <summary>Google OAuth client id (used to build the sign-in redirect, safe to publish).</summary>
    public string? GoogleClientId { get; init; }
}
