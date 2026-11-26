# TechXpress - متجر إلكتروني للأجهزة الإلكترونية

مشروع تخرج - مبادرة رائدي الرقمية المصرية (المسار الكامل Full Stack .NET)

## نظرة عامة
TechXpress هو موقع تجارة إلكترونية للأجهزة الإلكترونية يدعم ثلاثة أدوار:
- **العميل (Customer)**: تصفح المنتجات، إضافة إلى سلة التسوق، قائمة المفضلة، تقييم المنتجات المشتراة، إتمام الطلبات، الدفع عبر Stripe، **إعادة محاولة الدفع** من سجل الطلبات، عرض الملف الشخصي
- **البائع (Vendor)**: إدارة منتجاته، طلب انضمام، متابعة طلبات متجره (طلبات متجري)، **شحن طلباته بنفسه** (تجهيز → شحن مع شركة الشحن ورقم التتبع → تأكيد التوصيل)، عرض أرباحه، طلب سحب الأرباح
- **المدير (Admin)**: إدارة المنتجات والفئات والطلبات والبائعين وطلبات السحب ورسائل التواصل وإدارة المستخدمين (تفعيل/تعليق)
- **لوحة تحكم البائع (Vendor Dashboard)**: صفحة واحدة تجمع رصيد المحفظة، إجمالي المبيعات (يحتسب الطلبات المدفوعة والمشحونة والمُسلّمة فقط — غير المدفوعة والملغاة لا تُحتسب)، عدد المنتجات، الطلبات التي تحتاج شحن، آخر الطلبات والمنتجات والحركات المالية، مع شريط تنقّل موحّد لكل صفحات البائع
- **سجل طلبات السحب للبائع (Vendor)**: يرى البائع حالة طلبه (قيد المراجعة / معتمد / مرفوض) مع سبب الرفض وتاريخ المراجعة
- **سلة تسوق محفوظة (Persistent Cart)**: تُحفظ السلة في قاعدة البيانات لكل مستخدم وتُستعاد تلقائياً بعد تسجيل الدخول من أي جهاز، مع مزامنة فورية لعدّاد السلة، ومسحها بعد إتمام الطلب أو عند تسجيل الخروج
- **تصميم حديث متكامل (Modern UI)**: نظام تصميم موحّد بواجهة عربية RTL — متجر فاتح أنيق، لوحة إدارة داكنة بشريط جانبي، بطاقات وزوايا ناعمة وظلال خفيفة وحركات دقيقة، متجاوب بالكامل مع الجوال

## المستندات
- [Project Planning](docs/Project-Planning/Project-Planning-and-Management.md)
- [Requirements & Stakeholder Analysis](docs/Requirements/Requirements-Gathering.md)
- [System Analysis & Design](docs/System-Analysis-Design/README.md)
  - [Architecture](docs/System-Analysis-Design/01-Problem-Statement-and-Architecture.md)
  - [Use Cases](docs/System-Analysis-Design/02-Use-Cases.md)
  - [Database Design](docs/System-Analysis-Design/03-Database-Design.md)
  - [Behavior Diagrams](docs/System-Analysis-Design/04-Behavior-Diagrams.md)
  - [UI/UX Design](docs/System-Analysis-Design/05-UI-UX-Design.md)
- [Combined Documentation (PDF)](docs/TechXpress-Documentation.pdf)
- [Presentation (PDF)](docs/TechXpress-Presentation.pdf)
- [Demo Script](docs/Presentation/Demo-Script.md)

## الميزات الرئيسية
- **سجل طلبات السحب للمدير (Admin Payout History)**: تبويبات فلترة حسب الحالة مع عدّادات، بطاقات إحصائية، جدول DataTables كامل، ومسار تدقيق (Audit Trail) يحفظ من اعتمد/رفض الطلب ومتى ولماذا، مع إعادة المبلغ المحجوز تلقائياً عند الرفض
- **تسجيل الدخول عبر Google**: تسجيل/دخول سريع، ويُنشأ الحساب كعميل، ويُربط تلقائياً إن كان البريد مسجّلاً بالفعل. الميزة **معطّلة بأمان** إن لم تُضبط مفاتيح Google
- **واجهات حسب الدور**: عند تسجيل الدخول يُوجَّه المدير للوحة التحكم والبائع للوحة متجره، ويُخفى المتجر (المنتجات/السلة/المفضلة/البحث) عنهما لأنهما يعملان من لوحتيهما
- **قائمة المفضلة (Wishlist)**: حفظ المنتجات، نقلها إلى السلة، إزالتها، مع مؤشر توفّر المخزون
- **إعادة محاولة الدفع (Retry Payment)**: إذا فشل الدفع أو أُلغيت الجلسة، يبقى الطلب بحالة «بانتظار الدفع» مع زر **«ادفع الآن»** في سجل الطلبات وفي صفحة تفاصيل الطلب وصفحة إلغاء الدفع. عند إعادة المحاولة يستأنف النظام **نفس جلسة Stripe** إن كانت ما زالت مفتوحة (بدل إنشاء جلسة جديدة)، وإن كانت الجلسة قد اكتملت ودفعت بالفعل يؤكّد الطلب تلقائياً وينقله لصفحة النجاح — وبذلك لا يمكن الدفع مرتين لنفس الطلب. **سلة الشراء تظل كما هي** عند فشل أو إلغاء الدفع ولا تُفرّغ إلا بعد نجاحه، مع **صفحة مخصّصة لمعالجة أخطاء الدفع** عند تعذّر الوصول إلى Stripe (تتضمن إعادة المحاولة والعودة للسلة)
- **إجمالي المبيعات المؤكدة**: لوحة الإدارة تعرض بطاقة «إجمالي المبيعات المؤكدة»، ولوحة البائع تعرض إجمالي مبيعاته، وكلاهما يحتسب الطلبات المدفوعة والمشحونة والمُسلّمة فقط، ولا يحتسب الطلبات غير المدفوعة أو الملغاة
- **عمولة الموقع (Platform Commission)**: تُحتسب نسبة ثابتة (10% افتراضيًا) من قيمة كل طلب لصالح المنصة، وتُسجَّل لكل بند طلب، وتظهر كبطاقة مستقلة في لوحة الإدارة، وتُلغى تلقائيًا عند إلغاء أو إرجاع الطلب
- **تحليلات لوحة الإدارة (Admin Analytics)**: فلتر للسنة يعيد حساب إجمالي المبيعات والعمولة والمصروفات، مع قائمة المنتجات الأكثر مبيعًا، والمناطق الأعلى والأقل شراءً من عناوين الشحن، وإمكانية تسجيل المصروفات التشغيلية وحساب صافي الربح تلقائيًا
- **رصيد النقاط (Store Credit)**: عند إلغاء طلب مدفوع أو إرجاع طلب تم توصيله تُسترد قيمته كاملة (المدفوع نقدًا + الرصيد المستخدم) كنقاط في حساب العميل (1 نقطة = 1 دولار)، ويمكن استخدام الرصيد في أي عملية شراء لاحقة من صفحة إتمام الطلب، مع سجل حركات كامل وصفحة رصيد مستقلة
- **تقييمات المنتجات (Product Reviews)**: تقييم بالنجوم (1-5) + تعليق، مقصورة على **المشترين الموثّقين فقط** (من أكمل شراء المنتج)، ومراجعة واحدة لكل منتج لكل عميل، مع عرض متوسط التقييم
- **إدارة المستخدمين (Admin User Management)**: عرض كل المستخدمين بأدوارهم، تفعيل/تعليق الحسابات، ومنع تعليق آخر مدير نشط أو الحساب الذاتي، ومنع تسجيل الدخول للحسابات المعلّقة
- **طلبات المتجر للبائع (Seller Order Fulfillment)**: عرض الطلبات التي تحتوي منتجات البائع مجمّعة بالطلب، مع بيانات الشحن وإجماليات كل بائع. **البائع مسؤول عن شحن منتجاته**: بدء التجهيز، تأكيد الشحن مع اسم شركة الشحن ورقم التتبع (اختياري)، ثم تأكيد التوصيل. تُحدَّث حالة الشحن لكل بند، وتتقدّم حالة الطلب العامة تلقائيًا إلى «تم الشحن» / «تم التوصيل» عند اكتمال كل البائعين، ويظهر رقم التتبع للعميل والأدمن
- **الرقابة على المنتجات (Admin Product Oversight)**: تفعيل/إلغاء تفعيل أي منتج
- **صفحة من نحن (About)**: تعريف بالمنصة والرؤية والرسالة ومزايا الشراء
- **صفحة تواصل معنا (Contact)**: نموذج مراسلة بتحقق كامل من المدخلات يُحفظ في قاعدة البيانات، مع صندوق وارد للأدمن (تعليم كمقروءة/حذف) وعدّاد للرسائل غير المقروءة
- **استعادة كلمة المرور**: تدفّق كامل «نسيت كلمة المرور» + صفحة إعادة التعيين برمز آمن (ASP.NET Identity)، مع رسائل خطأ مترجمة للعربية. **ملاحظة**: لا يوجد مزود بريد في حدود المشروع، لذلك في بيئة التطوير يُعرض رابط إعادة التعيين مباشرةً على الشاشة، وفي الإنتاج يُسجّل الرابط في السجل (Log) مع رسالة عامة للمستخدم
- **صفحة 404 مخصّصة**: أي رابط غير موجود يعرض صفحة خطأ مصمّمة مع الحفاظ على كود الحالة 404
- **صور منتجات فعلية**: الصور محلية داخل `wwwroot/images/products` ومربوطة بالبيانات الأولية (Seed) ليعمل العرض على أي جهاز بدون إنترنت

## التقنيات المستخدمة (مسموح بها فقط)
- ASP.NET Core MVC
- Entity Framework Core 8
- ASP.NET Identity
- Stripe.net (Checkout Session، وضع الاختبار) - يُستخدم عبر HttpClient مباشرة بسبب مشاكل في أنواع SDK
- jQuery + DataTables + Toastr JS
- Bootstrap + CSS مخصص
- xUnit + Moq + SQLite في الذاكرة (لاختبارات البيانات والخدمات)
- Microsoft Azure (للنشر)

**غير مسموح باستخدامها**: SignalR، Redis، Chart.js، SweetAlert، Twilio/SendGrid، Blob Storage، AutoMapper، MediatR، أو أي مكتبات أخرى.

## بنية الحل (Solution)
| المشروع | الغرض |
|---------|--------|
| TechXpress.Data | كيانات البيانات، سياق قاعدة البيانات، المستودعات، Unit of Work |
| TechXpress.Services | طبقة الأعمال (Services)، نماذج DTO، واجهات الخدمات |
| TechXpress.Web | تطبيق ASP.NET Core MVC (واجهات المستخدم، المتحكمات) |
| TechXpress.Data.Tests | اختبارات تكامل للمستودعات باستخدام SQLite في الذاكرة |
| TechXpress.Services.Tests | اختبارات وحدات للخدمات باستخدام Moq وxUnit |

## هيكل المعمارية
Controller → Service → IUnitOfWork → Repository → DbContext

- لا تُستخدم DbContext أو IUnitOfWork مباشرة في المتحكمات
- لا تُستخدم أنواع ASP.NET في طبقة الخدمات
- الوصول غير المتزامن للبيانات مع `AsNoTracking()` لعمليات القراءة
- العمليات الكتابية متعددة الخطوات تستخدم Transactions مع try/catch، `ILogger<T>`، و `throw;`
- إجراءات POST بها `[ValidateAntiForgeryToken]`
- الإجراءات المحمية بها `[Authorize]` مع الأدوار الصحيحة
- معرف المستخدم يُؤخذ من `User.FindFirstValue(ClaimTypes.NameIdentifier)` وليس من النموذج أو الرابط
- لا تُرسل كيانات إلى العروض ( Views تستخدم DTOs)
- تعليقات XML على واجهات الخدمات العامة

## الإعدادات والتكوين (Configuration)

### appsettings.json
يحتوي على الأماكن المخصصة لهذه الإعدادات (قيم غير حساسة فقط — لا تُوضع أي أسرار هنا):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=TechXpressDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Platform": {
    "CommissionRate": 0.10
  },
  "Promotions": [
    { "Code": "SAVE10", "Percent": 10 },
    { "Code": "SAVE20", "Percent": 20 }
  ],
  "Stripe": {
    "SecretKey": "",
    "PublishableKey": ""
  },
  "Authentication": {
    "Google": {
      "ClientId": "",
      "ClientSecret": ""
    }
  },
  "AppSettings": {
    "BaseUrl": "https://localhost:7167",
    "AdminEmail": "admin@techxpress.com"
  }
}
```

> ملاحظة أمنية: `AppSettings:AdminPassword` **غير موجود** في `appsettings.json` عن قصد. لا يُنشئ التطبيق حساب المدير إلا إذا كانت كلمة المرور مضبوطة في User Secrets (محلياً) أو Azure App Settings (عند النشر). إذا لم تُضبط، يعمل الموقع طبيعياً بدون إنشاء حساب مدير.

### User Secrets (للمفاتيح السرية - لا تُرتكب في مستودع الكود)
يجب ضبط الأسرار في User Secrets لمشروع الـ Web:

```bash
cd TechXpress.Web

# سلسلة الاتصال (محلياً: SQL Server Express أو LocalDB)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=TechXpressDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"

# مفاتيح Stripe (وضع الاختبار)
dotnet user-secrets set "Stripe:SecretKey" "sk_test_YOUR_SECRET_KEY"
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_YOUR_PUBLISHABLE_KEY"

# كلمة مرور حساب المدير (تُنشأ مرة واحدة عند أول تشغيل)
dotnet user-secrets set "AppSettings:AdminPassword" "Admin@123"

# (اختياري) تسجيل الدخول عبر Google
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_CLIENT_SECRET"
```

UserSecretsId للمشروع web: `c1c2ed3b-cab2-4c00-9e7c-482267e62afc`

### تسجيل الدخول عبر Google (Google Sign-in)
1. من [Google Cloud Console](https://console.cloud.google.com/apis/credentials) أنشئ **OAuth 2.0 Client ID** من نوع *Web application*.
2. أضف روابط إعادة التوجيه المصرّح بها (Authorized redirect URIs):
   - محلياً (HTTPS — المسار الافتراضي): `https://localhost:7167/signin-google`
   - محلياً (اختياري لو شغّلت بروفايل http فقط): `http://localhost:5163/signin-google`
   - عند النشر: `https://your-app.azurewebsites.net/signin-google`
3. ضبط `Authentication:Google:ClientId` و `Authentication:Google:ClientSecret` في User Secrets.
4. زر «المتابعة باستخدام Google» يظهر دائماً في صفحتي الدخول والتسجيل؛ وإذا لم تُضبط المفاتيح يظهر معطّلاً مع تنبيه واضح، ولا يتأثر باقي الموقع.

سلوك الحساب: يُنشأ المستخدم الجديد بدور **عميل (Customer)**، وإذا كان البريد مسجّلاً بالفعل فيُربط حساب Google بالحساب القائم ثم يُسجَّل الدخول. ويمكن للعميل لاحقاً التقدّم للانضمام كمورد من صفحة «كن بائعاً».

> **الحسابات المعلّقة:** أي حساب يعلّقه الأدمن (`IsActive = false`) لا يستطيع الدخول لا بالبريد/كلمة المرور ولا عبر Google، ويُنهى أي جلسة نشطة له تلقائياً خلال دقيقتين (بفضل تحديث `SecurityStamp` والتحقق الدوري منه).

### الإعدادات المطلوبة في Azure App Service
عند النشر على Microsoft Azure App Service، أضف إعدادات التطبيق التالية في Azure Portal (Configuration → Application settings):

| المفتاح | القيمة | ملاحظات |
|--------|-------|---------|
| `ConnectionStrings__DefaultConnection` | سلسلة الاتصال بقاعدة بيانات Azure SQL | سلسلة اتصال كاملة |
| `Stripe__SecretKey` | مفتاح السري من Stripe Dashboard | وضع الاختبار أو الإنتاج |
| `Stripe__PublishableKey` | مفتاح النشر من Stripe Dashboard | وضع الاختبار أو الإنتاج |
| `AppSettings__AdminPassword` | كلمة مرور قوية | **مطلوبة** لإنشاء حساب المدير على قاعدة بيانات جديدة |
| `Authentication__Google__ClientId` | من Google Cloud Console | اختياري (لتشغيل زر Google) |
| `Authentication__Google__ClientSecret` | من Google Cloud Console | اختياري (لتشغيل زر Google) |
| `Platform__CommissionRate` | `0.10` | نسبة عمولة المنصة (مثلاً 10%) |
| `AppSettings__BaseUrl` | `https://your-app.azurewebsites.net` | رابط الموقع المباشر |
| `ASPNETCORE_ENVIRONMENT` | `Production` | لتشغيل صفحة الخطأ العامة + HSTS + Forwarded Headers |

### ملاحظة حول الحفظ في App_Data (Persistence)
مستندات البائعين تُخزّن خارج `wwwroot` في المسار `App_Data/vendor-docs` لأسباب أمنية (ليست قابلة للوصول عبر الرابط المباشر).

عند النشر على Azure App Service، يجب ملاحظة أن نظام الملفات في App Service **لا يستمر** بين إعادة التشغيل ما لم يتم تمكين "Persistent File Share" أو استخدام Azure Blob Storage. بما أن استخدام Blob Storage غير مسموح في هذا المشروع، يُنصح بما يلي:
1. تمكين Azure App Service **Persistent File Share** للمسار `App_Data`
2. أو النسخ الاحتياطي الدوري لمحتويات `App_Data/vendor-docs`

## الحسابات الافتراضية (لبيئة التطوير)

| الدور | البريد الإلكتروني | كلمة المرور |
|-------|----------------|-------------|
| مدير (Admin) | admin@techxpress.com | من الإعدادات `AppSettings:AdminPassword` (User Secrets / Azure) |
| بائع (Vendor) | vendor@techxpress.com | Vendor@123 (معتمد تلقائياً عبر DbInitializer) |

> عند أول تشغيل يُنشئ التطبيق الأدوار تلقائياً، وحساب المدير فقط إذا كانت `AppSettings:AdminPassword` مضبوطة. أما البائع التجريبي فيُنشأ من `DbInitializer` مع منتجاته.

## تشغيل المشروع محلياً

### المتطلبات المسبقة
- .NET 8 SDK
- SQL Server (Express / LocalDB محلياً) أو Azure SQL (للنشر)

### خطوات التشغيل
```bash
# استعادة الحزم
dotnet restore TechXpress.slnx

# ضبط الأسرار في User Secrets
cd TechXpress.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=TechXpressDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
dotnet user-secrets set "AppSettings:AdminPassword" "Admin@123"
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_..."

# بناء الحل وتشغيل الاختبارات
dotnet build ../TechXpress.slnx
dotnet test  ../TechXpress.slnx

# تشغيل التطبيق (يُنشئ قاعدة البيانات ويطبّق الـ Migrations تلقائياً عند البدء)
dotnet run
```

الرابط الافتراضي للتطوير: `http://localhost:5163` (و HTTPS: `https://localhost:7167`).

## تشغيل الاختبارات
```bash
dotnet test TechXpress.slnx
```

تغطي الاختبارات:
- **CartService**: إضافة منتج، تحديث الكمية، إزالة، عروض الخصم، سلة فارغة
- **CartPersistenceService**: حفظ السلة في قاعدة البيانات، الاستعادة، المسح، تجاهل المستخدم المجهول، ورفض السلة الأكبر من الحد
- **ProductService**: جلب المنتجات مع التصفية والترقيم، إنشاء، تحديث، إلغاء تفعيل
- **OrderService**: إنشاء طلب، تغيير الحالة (انتقالات مسموحة)، إلغاء الطلب واستعادة المخزون
- **WalletService**: معالجة العمولة، عكس العمولة، طلب سحب، اعتماد/رفض طلب السحب، تسجيل بيانات التدقيق (من اعتمد/رفض ومتى)، إعادة المبلغ المحجوز عند الرفض، وعدّادات طلبات السحب
- **VendorService**: طلب انضمام، قبول، رفض، فتح المستندات، وإحصائيات لوحة تحكم البائع (المنتجات والطلبات والمبيعات مع استثناء الطلبات غير المدفوعة والملغاة من الإجمالي)
- **WishlistService**: إضافة، منع التكرار، التوفّر، الإزالة، التحقق من الملكية
- **ContactService**: حفظ رسالة تواصل، التحقق من الحقول المطلوبة، تقليم القيم الطويلة لحدود الأعمدة، الترتيب من الأحدث، عدّاد غير المقروءة، التعليم كمقروءة، الحذف
- **ReviewService**: الشراء الموثّق، منع التكرار، صحة التقييم، حساب المتوسط
- **UserManagementService**: عرض المستخدمين بالأدوار، التفعيل، منع تعليق الحساب الذاتي وآخر مدير نشط
- **VendorDocumentStorage**: حفظ المستند، فتح، حذف، التحقق من الامتداد والحجم
- **VendorRepository**: جلب البائع حسب المستخدم، حسب الحالة، جميع البائعين
- **WalletRepository**: جلب المعاملات حسب البائع، طلبات السحب المعلقة، التحقق من الإيداع

## الهوية البصرية والتصميم (Design System)

- **نظام تصميم موحّد** في `wwwroot/css/site.css`: متغيّرات CSS (ألوان، مسافات، زوايا، ظلال)، أزرار Pill، نماذج، بطاقات منتجات وفئات، شرائح (Chips)، تنبيهات، جداول، ترقيم صفحات، وحركات دخول خفيفة.
- **واجهة المتجر**: خلفية فاتحة `#f4f6fb` مع أسطح بيضاء وحبر كحلي `#0b1220` ولمسة ذهبية `#f5b301`، بانر رئيسي داكن بتدرّجات ضوئية وصور منتجات، وشريط إحصائيات متداخل، وبطاقات فئات، وقسم مزايا، وبانر دعوة للبائعين.
- **لوحة الإدارة**: ثيم داكن (`body.theme-dark`) بشريط جانبي ثابت (Sidebar) وحالة تنقّل نشطة، وشريط علوي زجاجي، وقائمة منسدلة على الجوال.
- **لوحة البائع**: واجهة فاتحة متناسقة مع المتجر، شريط تنقّل فرعي (Tabs) وشارات حالة.
- **واجهات الحساب**: تصميم مقسّم (لوحة هوية داكنة + بطاقة نموذج بيضاء) لصفحات الدخول والتسجيل واستعادة كلمة المرور.
- **RTL كامل** بخطوط النظام (Segoe UI / Cairo / Noto Kufi Arabic) وبدون أي خطوط أو مكتبات خارجية.
- **وصولية**: تباين WCAG AA، حالات تركيز واضحة، `aria-label` لكل زر أيقوني، واحترام `prefers-reduced-motion`.

## الجودة وإمكانية الوصول (Quality & Accessibility)

- تم فحص الصفحات الرئيسية بأداة **Lighthouse** (إمكانية الوصول، أفضل الممارسات، SEO) وحصلت على **100/100/100** لكل من: الصفحة الرئيسية، المنتجات، تفاصيل المنتج، السلة، الدفع، الملف الشخصي، المفضلة، من نحن، تواصل معنا، سياسة الخصوصية، تسجيل الدخول، إنشاء الحساب، لوحة تحكم الإدارة، ولوحة تحكم البائع.
- تسلسل عناوين HTML هرمي صحيح (`h1 → h2 → h3`) في جميع الصفحات، مع استخدام كلاسات Bootstrap (`h5`/`h6`) للحفاظ على الحجم البصري دون كسر البنية الدلالية.
- تباين ألوان مطابق لمعيار **WCAG AA** للنصوص والأزرار فوق الأسطح الفاتحة (البطاقات البيضاء والأقسام الفاتحة).
- وصف `meta description` لكل صفحة (وأوصاف مخصّصة للمنتجات)، وملف `wwwroot/robots.txt` يمنع فهرسة المناطق الخاصة (الإدارة، الحساب، السلة، الطلبات، البائع).
- صفحات الخطأ: صفحة **404** مخصّصة بالعربية، وصفحة خطأ عامة في بيئة الإنتاج دون كشف تفاصيل تقنية.

## قائمة مراجعة النشر على Azure (Azure Deployment Checklist)

- [ ] إنشاء App Service على Azure
- [ ] إنشاء قاعدة بيانات Azure SQL وسلسلة الاتصال
- [ ] إضافة إعدادات التطبيق في Azure Portal (الجدول أعلاه) — بما فيها `AppSettings__AdminPassword`
- [ ] ضبط `ASPNETCORE_ENVIRONMENT=Production` (لتشغيل HSTS + صفحة الخطأ العامة + Forwarded Headers)
- [ ] (اختياري) ضبط مفاتيح Google وإضافة `https://your-app.azurewebsites.net/signin-google` في Authorized redirect URIs
- [ ] تمكين Persistent File Share لـ `App_Data` إذا لزم الأمر
- [ ] نشر الكود عبر GitHub Actions أو Azure CLI أو Visual Studio
- [ ] تطبيق الـ Migrations تلقائياً عند بدء التشغيل (`DbInitializer` يستدعي `Database.MigrateAsync()` وينشئ الأدوار وحساب المدير)
- [ ] التحقق من عمل Stripe Checkout في وضع الاختبار (بطاقة `4242 4242 4242 4242`، أي تاريخ مستقبلي، أي CVC)
- [ ] اختبار «إعادة محاولة الدفع»: إنشاء طلب، إلغاء الدفع من صفحة Stripe، ثم الضغط على «ادفع الآن» من سجل الطلبات
- [ ] تفعيل **HTTPS فقط** على App Service (TLS/SSL settings → HTTPS Only) — الموقع يعمل خلفه بـ HSTS وصفحة خطأ عامة وForwarded Headers
- [ ] ضبط `AppSettings__BaseUrl` على رابط الموقع الفعلي `https://...` (يُستخدم كاحتياطي لروابط عودة Stripe)
- [ ] **محدودية معروفة**: لا يوجد Stripe Webhook في هذا المشروع (لا يمكن اختباره محلياً بدون رابط عام). تأكيد الدفع يتم عبر صفحة النجاح ثم يُتحقق منه بسحب الجلسة من Stripe. لذلك إن أغلق العميل المتصفح **قبل** العودة لصفحة النجاح يبقى الطلب «بانتظار الدفع» رغم خصم المبلغ، وبمجرد ضغطه على «ادفع الآن» يتعرّف النظام على أن الجلسة مكتملة ومدفوعة ويؤكّد الطلب دون خصم جديد. للإنتاج الفعلي يُنصح بإضافة Webhook `checkout.session.completed`
- [ ] التأكد من عمل روابط المستندات (App_Data/vendor-docs)

## الترخيص
هذا المشروع جزء من مبادرة رائدي الرقمية المصرية (DEPI) - المسار الكامل Full Stack .NET.

> **ملاحظة عن صور المنتجات التجريبية**: الصور الموجودة في `wwwroot/images/products` مأخوذة من **Wikimedia Commons** وتُستخدم هنا لأغراض العرض التعليمي/التجريبي فقط. عند نشر المشروع تجارياً يجب استبدالها بصور المنتجات الفعلية الخاصة بالمتجر.
