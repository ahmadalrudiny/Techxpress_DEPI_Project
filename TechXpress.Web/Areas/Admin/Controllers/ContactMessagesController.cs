using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechXpress.Services.Interfaces;

namespace TechXpress.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ContactMessagesController : Controller
{
    private readonly IContactService _contactService;

    public ContactMessagesController(IContactService contactService)
    {
        _contactService = contactService;
    }

    /// <summary>قائمة رسائل «تواصل معنا» الواردة من الزوار والعملاء.</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var messages = await _contactService.GetAllAsync();
        return View(messages);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var result = await _contactService.MarkAsReadAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _contactService.DeleteAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
