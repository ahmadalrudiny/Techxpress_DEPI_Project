using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TechXpress.Data.Entities;

namespace TechXpress.Data.Context;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
        builder.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(18, 2);
        builder.Entity<Order>().Property(o => o.DiscountAmount).HasPrecision(18, 2);
        builder.Entity<Order>().Property(o => o.PromoCode).HasMaxLength(50);
        builder.Entity<Order>().Property(o => o.StripeSessionId).HasMaxLength(255);
        builder.Entity<OrderDetail>().Property(d => d.UnitPrice).HasPrecision(18, 2);

        builder.Entity<Product>().Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Entity<Category>().Property(c => c.Name).HasMaxLength(100).IsRequired();

        builder.Entity<Product>()
            .HasOne(p => p.Category).WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OrderDetail>()
            .HasOne(d => d.Product).WithMany()
            .HasForeignKey(d => d.ProductId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OrderDetail>(e =>
        {
            e.Property(d => d.Carrier).HasMaxLength(100);
            e.Property(d => d.TrackingNumber).HasMaxLength(100);
        });

        // ----- Vendor -----
        builder.Entity<Vendor>(e =>
        {
            e.Property(v => v.StoreName).HasMaxLength(100).IsRequired();
            e.Property(v => v.NationalIdPath).HasMaxLength(300).IsRequired();
            e.Property(v => v.CommercialRegisterPath).HasMaxLength(300).IsRequired();
            e.Property(v => v.TaxCardPath).HasMaxLength(300).IsRequired();
            e.Property(v => v.RejectionReason).HasMaxLength(500);
            e.Property(v => v.WalletBalance).HasPrecision(18, 2);
            e.HasIndex(v => v.UserId).IsUnique();   // مستخدم واحد = بائع واحد
            e.HasOne(v => v.User).WithOne().HasForeignKey<Vendor>(v => v.UserId);
        });

        builder.Entity<Product>()
            .HasOne(p => p.Vendor).WithMany(v => v.Products)
            .HasForeignKey(p => p.VendorId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OrderDetail>()
            .HasOne(d => d.Vendor).WithMany(v => v.OrderDetails)
            .HasForeignKey(d => d.VendorId).OnDelete(DeleteBehavior.Restrict);

        // ----- WalletTransaction -----
        builder.Entity<WalletTransaction>(e =>
        {
            e.Property(t => t.Amount).HasPrecision(18, 2);
            e.Property(t => t.BalanceAfter).HasPrecision(18, 2);
            e.Property(t => t.Description).HasMaxLength(300);
            e.Property(t => t.PayoutDetails).HasMaxLength(100);
            e.Property(t => t.ProcessedByUserId).HasMaxLength(450);
            e.Property(t => t.ProcessedByName).HasMaxLength(256);
            e.Property(t => t.RejectionReason).HasMaxLength(500);
            e.Property(t => t.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(t => t.Vendor).WithMany(v => v.WalletTransactions)
                .HasForeignKey(t => t.VendorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.OrderDetail).WithMany()
                .HasForeignKey(t => t.OrderDetailId).OnDelete(DeleteBehavior.Restrict);

            // منع إيداع أرباح نفس البند مرتين
            e.HasIndex(t => new { t.OrderDetailId, t.Type })
                .IsUnique()
                .HasFilter("[OrderDetailId] IS NOT NULL AND [Type] = 'Deposit'");
        });

        // ----- WishlistItem -----
        builder.Entity<WishlistItem>(e =>
        {
            e.HasOne(w => w.User).WithMany()
                .HasForeignKey(w => w.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(w => w.Product).WithMany(p => p.WishlistItems)
                .HasForeignKey(w => w.ProductId).OnDelete(DeleteBehavior.Cascade);

            // منتج واحد مرة واحدة لكل مستخدم
            e.HasIndex(w => new { w.UserId, w.ProductId }).IsUnique();
        });

        // ----- Review -----
        builder.Entity<Review>(e =>
        {
            e.Property(r => r.Comment).HasMaxLength(1000).IsRequired();
            e.HasOne(r => r.User).WithMany()
                .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Product).WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.Cascade);

            // مراجعة واحدة لكل منتج لكل عميل
            e.HasIndex(r => new { r.UserId, r.ProductId }).IsUnique();
        });

        // ----- ContactMessage -----
        builder.Entity<ContactMessage>(e =>
        {
            e.Property(c => c.FullName).HasMaxLength(150).IsRequired();
            e.Property(c => c.Email).HasMaxLength(256).IsRequired();
            e.Property(c => c.Phone).HasMaxLength(30);
            e.Property(c => c.Subject).HasMaxLength(200).IsRequired();
            e.Property(c => c.Message).HasMaxLength(2000).IsRequired();
            e.HasIndex(c => c.CreatedAt);
        });
    }
}