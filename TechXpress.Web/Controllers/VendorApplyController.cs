using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechXpress.Services.Interfaces;
using TechXpress.Web.Models;

namespace TechXpress.Web.Controllers;

[Authorize(Roles = "Customer")]
public class VendorApplyController : Controller
{
    private readonly IVendorService _vendorService;

    public VendorApplyController(IVendorService vendorService)
    {
        _vendorService = vendorService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Apply()
    {
        var status = await _vendorService.GetApplicationStatusAsync(CurrentUserId);
        if (status != null && status.Status != TechXpress.Data.Entities.VendorStatus.Rejected)
            return RedirectToAction(nameof(Status));

        return View(new VendorApplyViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(VendorApplyViewModel model)
    {
        var vendorId = await _vendorService.GetVendorIdByUserIdAsync(CurrentUserId);
        if (vendorId.HasValue)
            return RedirectToAction(nameof(Status));

        if (!ModelState.IsValid)
            return View(model);

        var nationalId = model.NationalId != null ? new TechXpress.Services.Models.UploadedFile(
            model.NationalId.OpenReadStream(), model.NationalId.FileName, model.NationalId.Length) : null;
        var commercialRegister = model.CommercialRegister != null ? new TechXpress.Services.Models.UploadedFile(
            model.CommercialRegister.OpenReadStream(), model.CommercialRegister.FileName, model.CommercialRegister.Length) : null;
        var taxCard = model.TaxCard != null ? new TechXpress.Services.Models.UploadedFile(
            model.TaxCard.OpenReadStream(), model.TaxCard.FileName, model.TaxCard.Length) : null;

        if (nationalId == null || commercialRegister == null || taxCard == null)
        {
            ModelState.AddModelError(string.Empty, "يجب رفع جميع المستندات المطلوبة (البطاقة الشخصية، السجل التجاري، والبطاقة الضريبية).");
            return View(model);
        }

        var request = new TechXpress.Services.Models.VendorApplicationRequest(
            model.StoreName, nationalId, commercialRegister, taxCard);

        var (success, message) = await _vendorService.ApplyAsync(CurrentUserId, request);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        TempData["SuccessMessage"] = "تم إرسال طلبك بنجاح وهو قيد المراجعة من فريق الإدارة.";
        return RedirectToAction(nameof(Status));
    }

    [HttpGet]
    public async Task<IActionResult> Status()
    {
        var status = await _vendorService.GetApplicationStatusAsync(CurrentUserId);
        if (status == null)
            return RedirectToAction(nameof(Apply));

        var model = new VendorApplicationStatusViewModel
        {
            VendorId = status.VendorId,
            StoreName = status.StoreName,
            Status = status.Status.ToString(),
            RejectionReason = status.RejectionReason,
            SubmittedAt = status.SubmittedAt
        };

        return View(model);
    }
}
