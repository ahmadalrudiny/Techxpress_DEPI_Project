using System.ComponentModel.DataAnnotations;

namespace TechXpress.Web.Models;

/// <summary>نموذج إرسال رسالة من صفحة «تواصل معنا».</summary>
public class ContactFormViewModel
{
    [Required(ErrorMessage = "الاسم مطلوب")]
    [StringLength(150, ErrorMessage = "الاسم يجب ألا يزيد عن 150 حرف")]
    [Display(Name = "الاسم بالكامل")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "صيغة البريد غير صحيحة")]
    [StringLength(256)]
    [Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "صيغة رقم الهاتف غير صحيحة")]
    [StringLength(30)]
    [Display(Name = "رقم الهاتف (اختياري)")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "موضوع الرسالة مطلوب")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "الموضوع يجب أن يكون بين 3 و200 حرف")]
    [Display(Name = "الموضوع")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "نص الرسالة مطلوب")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "الرسالة يجب أن تكون بين 10 و2000 حرف")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "الرسالة")]
    public string Message { get; set; } = string.Empty;
}
