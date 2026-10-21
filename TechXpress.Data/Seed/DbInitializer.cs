using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;

namespace TechXpress.Data.Seed;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // يطبّق أي Migrations ناقصة تلقائياً
        await context.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        await SeedAdminAsync(userManager, configuration);
        await SeedDemoVendorAsync(userManager, context);
        await SeedCategoriesAndProductsAsync(context);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { "Admin", "Vendor", "Customer" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        const string adminEmailKey = "AppSettings:AdminEmail";
        const string adminPasswordKey = "AppSettings:AdminPassword";

        var adminEmail = configuration[adminEmailKey] ?? "admin@techxpress.com";
        var adminPassword = configuration[adminPasswordKey] ?? "Admin@123456";

        if (await userManager.FindByEmailAsync(adminEmail) != null) return;

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Admin",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }

    private static async Task SeedDemoVendorAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        const string vendorEmail = "vendor@techxpress.com";
        const string vendorPassword = "Vendor@123";

        if (await userManager.FindByEmailAsync(vendorEmail) != null) return;

        var vendorUser = new ApplicationUser
        {
            UserName = vendorEmail,
            Email = vendorEmail,
            FullName = "Demo Vendor",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(vendorUser, vendorPassword);
        if (!result.Succeeded) return;

        await userManager.AddToRoleAsync(vendorUser, "Vendor");

        var vendor = new Vendor
        {
            UserId = vendorUser.Id,
            User = vendorUser,
            StoreName = "متجر التجريبي للأجهزة",
            NationalIdPath = "/files/demo-nid.pdf",
            CommercialRegisterPath = "/files/demo-cr.pdf",
            TaxCardPath = "/files/demo-tc.pdf",
            Status = VendorStatus.Approved,
            WalletBalance = 0m,
            SubmittedAt = DateTime.UtcNow.AddDays(-30),
            ReviewedAt = DateTime.UtcNow.AddDays(-29),
            ReviewedByUserId = "admin-user-id"
        };

        context.Vendors.Add(vendor);
        await context.SaveChangesAsync();

        // إضافة منتجات تابعة للبائع التجريبي
        var laptops = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Laptops");
        var mobiles = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Mobiles");

        if (laptops == null || mobiles == null) return;

        context.Products.AddRange(
            new Product
            {
                Name = "Demo Laptop Pro",
                Description = "لابتوب تجريبي للبائع التجريبي",
                Price = 1200m,
                StockQuantity = 10,
                CategoryId = laptops.Id,
                VendorId = vendor.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Name = "Demo Mobile X",
                Description = "هاتف محمول تجريبي للبائع التجريبي",
                Price = 600m,
                StockQuantity = 15,
                CategoryId = mobiles.Id,
                VendorId = vendor.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );

        await context.SaveChangesAsync();
    }

    private static async Task SeedCategoriesAndProductsAsync(ApplicationDbContext context)
    {
        if (await context.Categories.AnyAsync()) return;

        var laptops = new Category { Name = "Laptops", Description = "Laptops and notebooks" };
        var mobiles = new Category { Name = "Mobiles", Description = "Smartphones" };
        var cameras = new Category { Name = "Cameras", Description = "Digital cameras" };
        var accessories = new Category { Name = "Accessories", Description = "Headphones, chargers and more" };

        context.Categories.AddRange(laptops, mobiles, cameras, accessories);

        context.Products.AddRange(
            new Product { Name = "Dell XPS 13", Description = "13-inch ultrabook, 16GB RAM, 512GB SSD", Price = 1199, StockQuantity = 15, Category = laptops, ImageUrl = "/images/products/1.jpg" },
            new Product { Name = "MacBook Air M2", Description = "13-inch, 8GB RAM, 256GB SSD", Price = 1099, StockQuantity = 10, Category = laptops, ImageUrl = "/images/products/2.jpg" },
            new Product { Name = "Lenovo ThinkPad X1", Description = "14-inch business laptop", Price = 1399, StockQuantity = 8, Category = laptops, ImageUrl = "/images/products/3.jpg" },
            new Product { Name = "HP Pavilion 15", Description = "15.6-inch, 16GB RAM, 512GB SSD", Price = 749, StockQuantity = 20, Category = laptops, ImageUrl = "/images/products/4.jpg" },

            new Product { Name = "iPhone 15", Description = "128GB, 6.1-inch display", Price = 799, StockQuantity = 25, Category = mobiles, ImageUrl = "/images/products/5.jpg" },
            new Product { Name = "Samsung Galaxy S24", Description = "256GB, 6.2-inch display", Price = 849, StockQuantity = 18, Category = mobiles, ImageUrl = "/images/products/6.jpg" },
            new Product { Name = "Google Pixel 8", Description = "128GB, great camera", Price = 699, StockQuantity = 12, Category = mobiles, ImageUrl = "/images/products/7.jpg" },
            new Product { Name = "Xiaomi 13T", Description = "256GB, 144Hz display", Price = 499, StockQuantity = 30, Category = mobiles, ImageUrl = "/images/products/8.jpg" },

            new Product { Name = "Canon EOS R50", Description = "Mirrorless camera, 24MP", Price = 679, StockQuantity = 7, Category = cameras, ImageUrl = "/images/products/9.jpg" },
            new Product { Name = "Sony Alpha A6400", Description = "Mirrorless camera with 4K video", Price = 899, StockQuantity = 6, Category = cameras, ImageUrl = "/images/products/10.jpg" },
            new Product { Name = "Nikon Z50", Description = "20.9MP mirrorless camera", Price = 859, StockQuantity = 5, Category = cameras, ImageUrl = "/images/products/11.jpg" },

            new Product { Name = "Sony WH-1000XM5", Description = "Noise cancelling headphones", Price = 349, StockQuantity = 22, Category = accessories, ImageUrl = "/images/products/12.jpg" },
            new Product { Name = "Anker 65W Charger", Description = "Fast USB-C charger", Price = 39, StockQuantity = 50, Category = accessories, ImageUrl = "/images/products/13.jpg" },
            new Product { Name = "Logitech MX Master 3S", Description = "Wireless mouse", Price = 99, StockQuantity = 35, Category = accessories, ImageUrl = "/images/products/14.jpg" }
        );

        await context.SaveChangesAsync();
    }
}
