using Microsoft.EntityFrameworkCore;
using PizzaNight.Models;

namespace PizzaNight.Data;

public sealed class PizzaNightDbContext(DbContextOptions<PizzaNightDbContext> options) : DbContext(options)
{
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductOptionGroup> ProductOptionGroups => Set<ProductOptionGroup>();
    public DbSet<ProductOption> ProductOptions => Set<ProductOption>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemOption> OrderItemOptions => Set<OrderItemOption>();
    public DbSet<ShopSettings> ShopSettings => Set<ShopSettings>();
    public DbSet<ShopOpeningHour> ShopOpeningHours => Set<ShopOpeningHour>();
    public DbSet<DeliveryZone> DeliveryZones => Set<DeliveryZone>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MenuCategory>(entity =>
        {
            entity.HasIndex(category => category.Slug).IsUnique();
            entity.Property(category => category.Name).HasMaxLength(100);
            entity.Property(category => category.Slug).HasMaxLength(80);
            entity.HasMany(category => category.Products)
                .WithOne(product => product.MenuCategory)
                .HasForeignKey(product => product.MenuCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(product => product.Slug).IsUnique();
            entity.Property(product => product.Name).HasMaxLength(140);
            entity.Property(product => product.Slug).HasMaxLength(120);
            entity.Property(product => product.Description).HasMaxLength(600);
            entity.Property(product => product.ImagePath).HasMaxLength(260);
            entity.Property(product => product.Badge).HasMaxLength(60);
            entity.HasMany(product => product.OptionGroups)
                .WithOne(group => group.Product)
                .HasForeignKey(group => group.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductOptionGroup>(entity =>
        {
            entity.HasIndex(group => new { group.ProductId, group.Slug }).IsUnique();
            entity.Property(group => group.Name).HasMaxLength(100);
            entity.Property(group => group.Slug).HasMaxLength(80);
            entity.HasMany(group => group.Options)
                .WithOne(option => option.ProductOptionGroup)
                .HasForeignKey(option => option.ProductOptionGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductOption>(entity =>
        {
            entity.HasIndex(option => new { option.ProductOptionGroupId, option.Slug }).IsUnique();
            entity.Property(option => option.Name).HasMaxLength(100);
            entity.Property(option => option.Slug).HasMaxLength(80);
            entity.Property(option => option.Description).HasMaxLength(240);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(order => order.OrderNumber).IsUnique();
            entity.Property(order => order.OrderNumber).HasMaxLength(32);
            entity.Property(order => order.Type).HasConversion<string>().HasMaxLength(24);
            entity.Property(order => order.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(order => order.PaymentMethod).HasConversion<string>().HasMaxLength(24);
            entity.Property(order => order.PaymentStatus).HasConversion<string>().HasMaxLength(24);
            entity.Property(order => order.CustomerName).HasMaxLength(160);
            entity.Property(order => order.CustomerEmail).HasMaxLength(254);
            entity.Property(order => order.CustomerPhone).HasMaxLength(40);
            entity.Property(order => order.Postcode).HasMaxLength(16);
            entity.Property(order => order.AddressLine).HasMaxLength(300);
            entity.Property(order => order.DeliveryInstructions).HasMaxLength(600);
            entity.Property(order => order.OrderNotes).HasMaxLength(1000);
            entity.HasMany(order => order.Items)
                .WithOne(item => item.Order)
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(item => item.ProductName).HasMaxLength(140);
            entity.HasOne(item => item.Product)
                .WithMany()
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(item => item.Options)
                .WithOne(option => option.OrderItem)
                .HasForeignKey(option => option.OrderItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItemOption>(entity =>
        {
            entity.Property(option => option.Name).HasMaxLength(160);
        });

        modelBuilder.Entity<ShopSettings>(entity =>
        {
            entity.Property(settings => settings.TemporaryClosureMessage).HasMaxLength(240);
        });

        modelBuilder.Entity<ShopOpeningHour>(entity =>
        {
            entity.HasIndex(hours => hours.DayOfWeek).IsUnique();
            entity.Property(hours => hours.DayOfWeek).HasConversion<string>().HasMaxLength(12);
        });

        modelBuilder.Entity<DeliveryZone>(entity =>
        {
            entity.HasIndex(zone => zone.PostcodePrefix).IsUnique();
            entity.Property(zone => zone.Name).HasMaxLength(100);
            entity.Property(zone => zone.PostcodePrefix).HasMaxLength(12);
        });
    }
}
