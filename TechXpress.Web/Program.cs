using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;
using TechXpress.Data.Repositories.Implementations;
using TechXpress.Data.Repositories.Interfaces;
using TechXpress.Data.Seed;
using TechXpress.Data.UoW;
using TechXpress.Services.Implementations;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;
using TechXpress.Web.Infrastructure;
using TechXpress.Web.Options;

var builder = WebApplication.CreateBuilder(args);

// ===== تسجيل الأخطاء في ملفات يومية داخل مجلد logs =====
var logDirectory = Path.Combine(builder.Environment.ContentRootPath, "logs");
Directory.CreateDirectory(logDirectory);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine(logDirectory, "log-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 20 * 1024 * 1024,
        rollOnFileSizeLimit: true,
        shared: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// 1) الداتابيز
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2) الـ Repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

// 3) الـ Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// 4) الـ Identity - تخفيف شروط كلمة المرور لتسهيل الاختبار
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// توجيه مسار الكوكيز إلى AccountController
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;

    // في الإنتاج الكوكي لا تُرسل إلا عبر HTTPS
    if (!builder.Environment.IsDevelopment())
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }
});

// عند تعليق حساب من لوحة الأدمن يتغيّر SecurityStamp؛ هذا الفاصل يضمن إنهاء
// جلسات الحساب المعلّق خلال دقيقتين كحد أقصى بدل المدة الافتراضية (30 دقيقة).
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(2);
});

// تسجيل الدخول الخارجي (Google) — يُفعّل فقط عند وجود بيانات الاعتماد
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var googleEnabled = !string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret);

if (googleEnabled)
{
    builder.Services.AddAuthentication().AddGoogle(options =>
    {
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
        options.CallbackPath = "/signin-google";
    });
}

builder.Services.AddSingleton(new ExternalAuthOptions
{
    GoogleEnabled = googleEnabled,
    GoogleClientId = googleClientId
});

// 5) الـ Services
// عميل HTTP خاص بـ Stripe عبر IHttpClientFactory (تجميع الاتصالات بدل إنشاء HttpClient لكل طلب)
builder.Services.AddHttpClient(PaymentService.HttpClientName, client =>
{
    client.BaseAddress = new Uri("https://api.stripe.com/");
    client.DefaultRequestHeaders.Add("Stripe-Version", "2023-10-16");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IStoreCreditService, StoreCreditService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICartPersistenceService, CartPersistenceService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAdminAnalyticsService, AdminAnalyticsService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IUserManagementRepository, UserManagementRepository>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IVendorProductService>(sp =>
    new VendorProductService(
        sp.GetRequiredService<IUnitOfWork>(),
        sp.GetRequiredService<ILogger<VendorProductService>>(),
        Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "images", "products")));

// مسار تخزين المستندات: خارج wwwroot فمش بيتفتح برابط مباشر
builder.Services.AddSingleton<IVendorDocumentStorage>(sp =>
    new VendorDocumentStorage(
        Path.Combine(builder.Environment.ContentRootPath, "App_Data", "vendor-docs"),
        sp.GetRequiredService<ILogger<VendorDocumentStorage>>()));

builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<IContactService, ContactService>();

// 6) الـ MVC
builder.Services.AddControllersWithViews();

// Session for cart
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    if (!builder.Environment.IsDevelopment())
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// تجهيز البيانات والأدوار وحساب الأدمن الافتراضي عند بدء التشغيل
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DbInitializer.SeedAsync(services);

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    // إنشاء الأدوار الرئيسية
    string[] roles = { "Admin", "Vendor", "Customer" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // إنشاء حساب أدمن افتراضي مع إضافة FullName لتفادي خطأ SQL
    // لأسباب أمنية: لا يتم إنشاء الحساب إلا عند ضبط كلمة المرور في الإعدادات (User Secrets / Azure App Settings)
    var adminEmail = builder.Configuration["AppSettings:AdminEmail"] ?? "admin@techxpress.com";
    var adminPassword = builder.Configuration["AppSettings:AdminPassword"];
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null && !string.IsNullOrWhiteSpace(adminPassword))
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "مدير النظام",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

// ===== تسجيل كل طلب: المسار + النتيجة + الوقت + المستخدم + IP =====
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} -> {StatusCode} in {Elapsed:0.0000} ms | User: {UserName} | IP: {ClientIP}";
    options.GetLevel = (httpContext, elapsed, ex) =>
        ex != null || httpContext.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : LogEventLevel.Information;
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("UserName", httpContext.User?.Identity?.Name ?? "anonymous");
        diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "-");
    };
});

// خلف بروكسي الاستضافة (Azure App Service) بنقرأ ترويسات X-Forwarded-*
// لازم يكون أول Middleware حتى يصحّ Scheme/Host قبل HSTS وإعادة التوجيه
if (!app.Environment.IsDevelopment())
{
    var forwardedOptions = new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                         | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
    };
    // Azure بتغيّر عناوين البروكسي، فبنمسح القيود الافتراضية عشان نثق في البروكسي بتاع الاستضافة
    forwardedOptions.KnownNetworks.Clear();
    forwardedOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedOptions);
}

// بيئة الإنتاج: صفحة خطأ عامة بدون تفاصيل داخلية + HSTS
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ===== تسجيل تفاصيل أي Exception غير متوقع (المسار/المستخدم/Stack Trace) قبل معالجته =====
app.UseMiddleware<ExceptionLoggingMiddleware>();

// صفحة 404 مخصّصة: تُعاد فقط للردود الفارغة (بدون جسم)، لذلك لا تُخفي تفاصيل أي خطأ
app.UseStatusCodePagesWithReExecute("/Home/HttpStatus/{0}");

// ترويسات أمان أساسية لكل الردود: منع تخمين نوع المحتوى، وتقييد التأطير والمراجع والصلاحيات
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "SAMEORIGIN";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// 1. مسار المناطق (Areas Route)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// 2. المسار الافتراضي (Default Route)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "توقف التطبيق بشكل غير متوقع");
}
finally
{
    Log.CloseAndFlush();
}