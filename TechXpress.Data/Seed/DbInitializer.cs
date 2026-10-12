using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;

namespace TechXpress.Data.Seed;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // يطبّق أي Migrations ناقصة تلقائياً
        await context.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        await SeedAdminAsync(userManager);
        await SeedCategoriesAndProductsAsync(context);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { "Admin", "Customer" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager)
    {
        const string adminEmail = "admin@techxpress.com";
        const string adminPassword = "Admin@123";

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

    private static async Task SeedCategoriesAndProductsAsync(ApplicationDbContext context)
    {
        if (await context.Categories.AnyAsync()) return;

        var laptops = new Category { Name = "Laptops", Description = "Laptops and notebooks" };
        var mobiles = new Category { Name = "Mobiles", Description = "Smartphones" };
        var cameras = new Category { Name = "Cameras", Description = "Digital cameras" };
        var accessories = new Category { Name = "Accessories", Description = "Headphones, chargers and more" };

        context.Categories.AddRange(laptops, mobiles, cameras, accessories);

        context.Products.AddRange(
            new Product { Name = "Dell XPS 13", Description = "13-inch ultrabook, 16GB RAM, 512GB SSD", Price = 1199, StockQuantity = 15, Category = laptops },
            new Product { Name = "MacBook Air M2", Description = "13-inch, 8GB RAM, 256GB SSD", Price = 1099, StockQuantity = 10, Category = laptops },
            new Product { Name = "Lenovo ThinkPad X1", Description = "14-inch business laptop", Price = 1399, StockQuantity = 8, Category = laptops },
            new Product { Name = "HP Pavilion 15", Description = "15.6-inch, 16GB RAM, 512GB SSD", Price = 749, StockQuantity = 20, Category = laptops },

            new Product { Name = "iPhone 15", Description = "128GB, 6.1-inch display", Price = 799, StockQuantity = 25, Category = mobiles },
            new Product { Name = "Samsung Galaxy S24", Description = "256GB, 6.2-inch display", Price = 849, StockQuantity = 18, Category = mobiles },
            new Product { Name = "Google Pixel 8", Description = "128GB, great camera", Price = 699, StockQuantity = 12, Category = mobiles },
            new Product { Name = "Xiaomi 13T", Description = "256GB, 144Hz display", Price = 499, StockQuantity = 30, Category = mobiles },

            new Product { Name = "Canon EOS R50", Description = "Mirrorless camera, 24MP", Price = 679, StockQuantity = 7, Category = cameras },
            new Product { Name = "Sony Alpha A6400", Description = "Mirrorless camera with 4K video", Price = 899, StockQuantity = 6, Category = cameras },
            new Product { Name = "Nikon Z50", Description = "20.9MP mirrorless camera", Price = 859, StockQuantity = 5, Category = cameras },

            new Product { Name = "Sony WH-1000XM5", Description = "Noise cancelling headphones", Price = 349, StockQuantity = 22, Category = accessories },
            new Product { Name = "Anker 65W Charger", Description = "Fast USB-C charger", Price = 39, StockQuantity = 50, Category = accessories },
            new Product { Name = "Logitech MX Master 3S", Description = "Wireless mouse", Price = 99, StockQuantity = 35, Category = accessories }
        );

        await context.SaveChangesAsync();
    }
}