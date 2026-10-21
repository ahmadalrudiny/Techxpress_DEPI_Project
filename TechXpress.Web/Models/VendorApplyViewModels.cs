using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TechXpress.Web.Models;

public class VendorApplyViewModel
{
    [Required]
    [StringLength(100)]
    public string StoreName { get; set; } = string.Empty;

    [Required]
    public IFormFile NationalId { get; set; } = null!;

    [Required]
    public IFormFile CommercialRegister { get; set; } = null!;

    [Required]
    public IFormFile TaxCard { get; set; } = null!;
}

public class VendorApplicationStatusViewModel
{
    public int VendorId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime SubmittedAt { get; set; }
}
