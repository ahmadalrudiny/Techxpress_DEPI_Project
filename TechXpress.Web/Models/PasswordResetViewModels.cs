using System.ComponentModel.DataAnnotations;

namespace TechXpress.Web.Models;

/// <summary>نموذج طلب رابط إعادة تعيين كلمة المرور.</summary>
public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "صيغة البريد غير صحيحة")]
    [Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>نموذج تعيين كلمة مرور جديدة باستخدام رمز إعادة التعيين.</summary>
public class ResetPasswordViewModel
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "كلمة المرور يجب ألا تقل عن 6 أحرف")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور الجديدة")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "كلمة المرور وتأكيدها غير متطابقين")]
    [Display(Name = "تأكيد كلمة المرور")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
