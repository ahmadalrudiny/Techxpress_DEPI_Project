using System.ComponentModel.DataAnnotations;

namespace TechXpress.Web.Models;

public class CreateReviewViewModel
{
    [Required]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "اختر عدد النجوم.")]
    [Range(1, 5, ErrorMessage = "التقييم يجب أن يكون بين 1 و 5 نجوم.")]
    public int Rating { get; set; } = 5;

    [Required(ErrorMessage = "اكتب تعليقك.")]
    [StringLength(1000, MinimumLength = 3, ErrorMessage = "التعليق يجب أن يكون بين 3 و 1000 حرف.")]
    public string Comment { get; set; } = string.Empty;
}
