using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;
using TechXpress.Web.Models;

namespace TechXpress.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IContactService _contactService;
    private readonly IWebHostEnvironment _environment;

    public HomeController(
        ILogger<HomeController> logger,
        IProductService productService,
        ICategoryService categoryService,
        IContactService contactService,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _productService = productService;
        _categoryService = categoryService;
        _contactService = contactService;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        // إذا لم يكن المستخدم مسجل دخول، يذهب لصفحة تسجيل الدخول أولاً
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Home") });
        }

        // جلب أحدث 8 منتجات نشطة للعرض في الصفحة الرئيسية
        var (products, totalCount) = await _productService.GetPagedAsync(page: 1, pageSize: 8, sort: "newest");

        var categories = await _categoryService.GetAllAsync();

        var viewModel = new HomeIndexViewModel
        {
            FeaturedProducts = products,
            Categories = categories,
            TotalProducts = totalCount
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>صفحة «من نحن».</summary>
    public IActionResult About() => View();

    /// <summary>صفحة «تواصل معنا» — نموذج إرسال رسالة لفريق الدعم.</summary>
    [HttpGet]
    public IActionResult Contact() => View(new ContactFormViewModel());

    /// <summary>استقبال رسالة «تواصل معنا» وحفظها لعرضها في لوحة الأدمن.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _contactService.SendAsync(
            model.FullName, model.Email, model.Phone, model.Subject, model.Message);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

        if (!result.Success)
        {
            return View(model);
        }

        return RedirectToAction(nameof(Contact));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        // تفاصيل الخطأ تُعرض في بيئة التطوير فقط، وفي الإنتاج رسالة عامة
        if (_environment.IsDevelopment())
        {
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            if (exceptionFeature?.Error != null)
            {
                _logger.LogError(exceptionFeature.Error, "Unhandled exception on {Path}", exceptionFeature.Path);
                ViewBag.ErrorMessage = exceptionFeature.Error.Message;
                ViewBag.InnerMessage = exceptionFeature.Error.InnerException?.Message;
                ViewBag.StackTrace = exceptionFeature.Error.StackTrace;
            }
        }

        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    /// <summary>
    /// صفحة الحالة (Status Code Page) — تُستدعى عبر UseStatusCodePagesWithReExecute.
    /// اسم الوسيط <c>id</c> مطابق لاسم الفتحة في المسار ({id?}) ولذلك يُمرَّر كود الحالة صحيحاً.
    /// </summary>
    public IActionResult HttpStatus(int id)
    {
        if (id == 404)
        {
            // الحفاظ على كود الحالة الأصلي (404) مع عرض صفحة مخصّصة
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("PageNotFound");
        }

        Response.StatusCode = id is >= 400 and < 600 ? id : StatusCodes.Status500InternalServerError;

        // تفاصيل الاستثناء تُعرض في بيئة التطوير فقط — لا نكشف أي تفاصيل داخلية في الإنتاج
        if (_environment.IsDevelopment())
        {
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionFeature?.Error != null)
            {
                ViewBag.ErrorMessage = exceptionFeature.Error.Message;
                ViewBag.InnerMessage = exceptionFeature.Error.InnerException?.Message;
                ViewBag.StackTrace = exceptionFeature.Error.StackTrace;
            }
        }

        return View("Error", new ErrorViewModel
        {
            RequestId = HttpContext.TraceIdentifier
        });
    }
}
