using System.ComponentModel.DataAnnotations;

namespace TechXpress.Web.Models;

public class ProfileViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>رصيد النقاط المتاح للعميل.</summary>
    public decimal StoreCreditBalance { get; set; }
}

public class EditProfileViewModel
{
    [Required]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Address")]
    public string Address { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Display(Name = "City")]
    [StringLength(100)]
    public string? City { get; set; }

    [Display(Name = "Postal Code")]
    [StringLength(20)]
    public string? PostalCode { get; set; }
}

public class ChangePasswordViewModel
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
