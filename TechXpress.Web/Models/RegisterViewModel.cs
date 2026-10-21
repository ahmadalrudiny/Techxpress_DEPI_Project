using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TechXpress.Web.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "الاسم بالكامل مطلوب")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "صيغة البريد غير صحيحة")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "كلمة المرور يجب ألا تقل عن 6 أحرف")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "كلمة المرور وتأكيدها غير متطابقين")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "Customer"; // Customer or Vendor

    /// <summary>Store name — required only when registering as a vendor.</summary>
    [StringLength(100, ErrorMessage = "اسم المتجر يجب ألا يزيد عن 100 حرف")]
    public string? StoreName { get; set; }

    /// <summary>National ID / personal card image — required only when registering as a vendor.</summary>
    [Display(Name = "صورة البطاقة الشخصية")]
    public IFormFile? NationalId { get; set; }

    /// <summary>Commercial register document — required only when registering as a vendor.</summary>
    [Display(Name = "السجل التجاري")]
    public IFormFile? CommercialRegister { get; set; }

    /// <summary>Tax card document — required only when registering as a vendor.</summary>
    [Display(Name = "البطاقة الضريبية")]
    public IFormFile? TaxCard { get; set; }
}
