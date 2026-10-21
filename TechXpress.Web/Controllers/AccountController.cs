using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TechXpress.Data.Entities;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;
using TechXpress.Web.Infrastructure;
using TechXpress.Web.Models;
using TechXpress.Web.Options;

namespace TechXpress.Web.Controllers;

public class AccountController : Controller
{
    /// <summary>Extensions accepted for vendor verification documents.</summary>
    private static readonly string[] AllowedDocumentExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
    private const long MaxDocumentBytes = 5 * 1024 * 1024; // 5 MB
    private const string GoogleProvider = "Google";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IVendorService _vendorService;
    private readonly ILogger<AccountController> _logger;
    private readonly ExternalAuthOptions _externalAuth;
    private readonly IWebHostEnvironment _environment;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IVendorService vendorService,
        ILogger<AccountController> logger,
        ExternalAuthOptions externalAuth,
        IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _vendorService = vendorService;
        _logger = logger;
        _externalAuth = externalAuth;
        _environment = environment;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // المدير المسجل دخوله بالفعل يذهب مباشرة إلى لوحة التحكم
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

            if (User.IsInRole("Vendor"))
                return RedirectToAction("Index", "VendorDashboard");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user != null)
        {
            // منع تسجيل الدخول للحسابات المعلّقة من الأدمن
            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "هذا الحساب معلّق. تواصل مع الدعم الفني.");
                return View(model);
            }

            // lockoutOnFailure: true يمنع تخمين كلمات المرور
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} logged in with a password", user.Id);
                return await RedirectAfterLoginAsync(user, returnUrl);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {UserId} locked out", user.Id);
                ModelState.AddModelError(string.Empty, "تم قفل الحساب مؤقتاً بسبب محاولات كثيرة. حاول لاحقاً.");
                return View(model);
            }
        }

        ModelState.AddModelError(string.Empty, "البريد الإلكتروني أو كلمة المرور غير صحيحة.");
        return View(model);
    }

    // ================= نسيت كلمة المرور / إعادة التعيين =================

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    /// <summary>
    /// يطلب رابط إعادة تعيين كلمة المرور. لا يكشف إن كان البريد مسجّلاً أم لا (حماية من تعداد الحسابات)،
    /// ولا يرسل بريداً فعلياً لعدم وجود مزود بريد في هذه النسخة؛ في بيئة التطوير يُعرض الرابط مباشرة للتجربة.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user != null && user.IsActive)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var link = Url.Action(nameof(ResetPassword), "Account",
                new { email = user.Email, token }, Request.Scheme);

            if (_environment.IsDevelopment() && link != null)
            {
                ViewData["ResetLink"] = link;
                _logger.LogInformation("Password reset link generated for {Email} (Development only)", model.Email);
            }
            else
            {
                _logger.LogWarning("Password reset requested for {Email} but no email provider is configured", model.Email);
            }
        }

        return View("ForgotPasswordConfirmation");
    }

    [HttpGet]
    public IActionResult ResetPassword(string? email, string? token, string? code)
    {
        token ??= code;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            TempData["ErrorMessage"] = "رابط إعادة تعيين كلمة المرور غير صالح.";
            return RedirectToAction(nameof(Login));
        }

        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "رابط إعادة التعيين غير صالح أو انتهت صلاحيته.");
            return View(model);
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "تم إعادة تعيين كلمة المرور بنجاح، يمكنك تسجيل الدخول الآن.";
            _logger.LogInformation("Password reset succeeded for {Email}", model.Email);
            return RedirectToAction(nameof(Login));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, TranslateIdentityError(error));

        return View(model);
    }

    /// <summary>ترجمة أخطاء Identity الشائعة إلى رسائل عربية واضحة.</summary>
    private static string TranslateIdentityError(IdentityError error) => error.Code switch
    {
        "PasswordTooShort" => "كلمة المرور قصيرة جداً.",
        "PasswordRequiresNonAlphanumeric" => "كلمة المرور يجب أن تحتوي على رمز خاص (مثل @ أو #).",
        "PasswordRequiresDigit" => "كلمة المرور يجب أن تحتوي على رقم واحد على الأقل.",
        "PasswordRequiresLower" => "كلمة المرور يجب أن تحتوي على حرف صغير واحد على الأقل.",
        "PasswordRequiresUpper" => "كلمة المرور يجب أن تحتوي على حرف كبير واحد على الأقل.",
        "PasswordMismatch" => "كلمة المرور الحالية غير صحيحة.",
        "InvalidToken" => "رابط إعادة التعيين غير صالح أو انتهت صلاحيته، اطلب رابطاً جديداً.",
        "DuplicateUserName" or "DuplicateEmail" => "هذا البريد الإلكتروني مسجّل بالفعل.",
        _ => error.Description
    };

    /// <summary>
    /// يوجّه المستخدم بعد تسجيل الدخول: المدير للوحة التحكم، والبائع للوحة متجره،
    /// والعميل لـ returnUrl إن كان محلياً وإلا للصفحة الرئيسية.
    /// </summary>
    private async Task<IActionResult> RedirectAfterLoginAsync(ApplicationUser user, string? returnUrl)
    {
        if (await _userManager.IsInRoleAsync(user, "Admin"))
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

        if (await _userManager.IsInRoleAsync(user, "Vendor"))
            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction("Index", "VendorDashboard");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    // ===== تسجيل الدخول عبر مزوّد خارجي (Google) =====

    /// <summary>يبدأ رحلة تسجيل الدخول الخارجي (Google).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        if (!IsProviderEnabled(provider))
        {
            TempData["ErrorMessage"] =
                "تسجيل الدخول عبر Google غير مُفعّل حالياً. يمكنك إنشاء حساب بالبريد الإلكتروني، أو تفعيل Google بإضافة مفاتيح OAuth في إعدادات الموقع.";
            return RedirectToAction(nameof(Login));
        }

        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    /// <summary>يعود إليه مزوّد الهوية بعد نجاح/فشل المصادقة.</summary>
    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (!string.IsNullOrEmpty(remoteError))
        {
            _logger.LogWarning("External login failed: {Error}", remoteError);
            TempData["ErrorMessage"] = $"تعذّر تسجيل الدخول عبر {GoogleProvider}. حاول مرة أخرى.";
            return RedirectToAction(nameof(Login));
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            TempData["ErrorMessage"] = "تعذّر قراءة بيانات المزوّد الخارجي. حاول مرة أخرى.";
            return RedirectToAction(nameof(Login));
        }

        // 1) حساب مرتبط بالفعل بهذا المزوّد
        var signInResult = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

        if (signInResult.Succeeded)
        {
            var linked = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (linked != null)
            {
                // حساب معلّق من الأدمن لا يُسمح له بالدخول عبر Google (نفس قاعدة الدخول بالبريد)
                if (!linked.IsActive)
                {
                    await _signInManager.SignOutAsync();
                    TempData["ErrorMessage"] = "هذا الحساب معلّق. تواصل مع الدعم الفني.";
                    return RedirectToAction(nameof(Login));
                }

                _logger.LogInformation("User {UserId} logged in with {Provider}", linked.Id, info.LoginProvider);
                return await RedirectAfterLoginAsync(linked, returnUrl);
            }
        }

        if (signInResult.IsLockedOut)
        {
            TempData["ErrorMessage"] = "هذا الحساب معلّق. تواصل مع الدعم الفني.";
            return RedirectToAction(nameof(Login));
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["ErrorMessage"] = "لم نتمكن من الحصول على بريدك الإلكتروني من المزوّد.";
            return RedirectToAction(nameof(Login));
        }

        var displayName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email;

        // 2) بريد موجود بالفعل: نربط الحساب بالمزوّد ثم نسجّل الدخول
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            if (!existingUser.IsActive)
            {
                TempData["ErrorMessage"] = "هذا الحساب معلّق. تواصل مع الدعم الفني.";
                return RedirectToAction(nameof(Login));
            }

            var linkResult = await _userManager.AddLoginAsync(existingUser, info);
            if (!linkResult.Succeeded)
            {
                _logger.LogWarning("Could not link {Provider} to user {UserId}: {Errors}",
                    info.LoginProvider, existingUser.Id,
                    string.Join(", ", linkResult.Errors.Select(e => e.Description)));
                TempData["ErrorMessage"] = "تعذّر ربط حساب Google بحسابك الحالي.";
                return RedirectToAction(nameof(Login));
            }

            await _signInManager.SignInAsync(existingUser, isPersistent: false);
            TempData["SuccessMessage"] = "تم ربط حساب Google وتسجيل الدخول بنجاح.";
            return await RedirectAfterLoginAsync(existingUser, returnUrl);
        }

        // 3) مستخدم جديد: يُنشأ كعميل، ويمكنه التقدّم لاحقاً للانضمام كمورد
        var newUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = displayName,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(newUser);
        if (!createResult.Succeeded)
        {
            _logger.LogError("Could not create external user {Email}: {Errors}",
                email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
            TempData["ErrorMessage"] = "تعذّر إنشاء الحساب. حاول مرة أخرى أو استخدم التسجيل العادي.";
            return RedirectToAction(nameof(Register));
        }

        await _userManager.AddLoginAsync(newUser, info);
        await _userManager.AddToRoleAsync(newUser, "Customer");
        await _signInManager.SignInAsync(newUser, isPersistent: false);

        _logger.LogInformation("New customer created via {Provider}: {UserId}", info.LoginProvider, newUser.Id);
        TempData["SuccessMessage"] = "تم إنشاء حسابك عبر Google بنجاح! يمكنك الآن التصفّح والشراء، أو التقدّم للانضمام كمورد.";
        return await RedirectAfterLoginAsync(newUser, returnUrl);
    }

    private bool IsProviderEnabled(string provider) =>
        _externalAuth.GoogleEnabled && string.Equals(provider, GoogleProvider, StringComparison.OrdinalIgnoreCase);

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        var isVendor = string.Equals(model.Role, "Vendor", StringComparison.OrdinalIgnoreCase);

        // ── التحقق من مستندات الانضمام كمورد (قبل إنشاء الحساب) ──
        if (isVendor)
            ValidateVendorApplication(model);

        if (!ModelState.IsValid)
            return View(model);

        if (await _userManager.FindByEmailAsync(model.Email) != null)
        {
            ModelState.AddModelError(string.Empty, "هذا البريد الإلكتروني مُسجل بالفعل.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        // كل تسجيل جديد = Customer دايماً. صلاحية Vendor بتُمنح بعد موافقة الأدمن
        await _userManager.AddToRoleAsync(user, "Customer");

        await _signInManager.SignInAsync(user, isPersistent: false);
        _logger.LogInformation("New {Role} registered: {UserId}", isVendor ? "vendor applicant" : "customer", user.Id);

        if (isVendor)
        {
            var request = new VendorApplicationRequest(
                model.StoreName!.Trim(),
                ToUploadedFile(model.NationalId!),
                ToUploadedFile(model.CommercialRegister!),
                ToUploadedFile(model.TaxCard!));

            var (success, message) = await _vendorService.ApplyAsync(user.Id, request);

            if (success)
            {
                TempData["SuccessMessage"] = "تم إنشاء حسابك وإرسال طلب الانضمام كمورد، وهو قيد المراجعة من فريق الإدارة.";
                return RedirectToAction("Status", "VendorApply");
            }

            // الحساب اتعمل بالفعل لكن المستندات اترفضت (نوع/حجم غير مسموح) - يكمل من صفحة التقديم
            TempData["ErrorMessage"] = message;
            return RedirectToAction("Apply", "VendorApply");
        }

        TempData["SuccessMessage"] = "تم إنشاء الحساب بنجاح!";
        return RedirectToAction("Index", "Home");
    }

    /// <summary>Adds model errors when a vendor registration is missing or has invalid documents.</summary>
    private void ValidateVendorApplication(RegisterViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.StoreName))
            ModelState.AddModelError(nameof(model.StoreName), "اسم المتجر مطلوب للانضمام كمورد.");

        ValidateDocument(model.NationalId, nameof(model.NationalId), "صورة البطاقة الشخصية");
        ValidateDocument(model.CommercialRegister, nameof(model.CommercialRegister), "السجل التجاري");
        ValidateDocument(model.TaxCard, nameof(model.TaxCard), "البطاقة الضريبية");
    }

    private void ValidateDocument(IFormFile? file, string key, string label)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(key, $"{label}: الملف مطلوب للانضمام كمورد.");
            return;
        }

        if (file.Length > MaxDocumentBytes)
            ModelState.AddModelError(key, $"{label}: حجم الملف يجب ألا يزيد عن 5 ميجابايت.");
        else if (!AllowedDocumentExtensions.Contains(Path.GetExtension(file.FileName), StringComparer.OrdinalIgnoreCase))
            ModelState.AddModelError(key, $"{label}: الأنواع المسموحة PDF أو JPG أو PNG.");
    }

    private static UploadedFile ToUploadedFile(IFormFile file) =>
        new(file.OpenReadStream(), file.FileName, file.Length);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        // لا نترك سلة المستخدم في الجلسة للمستخدم التالي على نفس المتصفح
        HttpContext.Session.Remove(CartSession.Key);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();
}
