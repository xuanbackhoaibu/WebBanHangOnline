using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Models;
using System;
using System.Security.Claims;
using System.Text.Json;

namespace WebBanHangOnline.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private static readonly HashSet<string> AuditedEntities =
        [
            nameof(Product),
            nameof(ProductVariant),
            nameof(Category),
            nameof(Order),
            nameof(OrderDetail),
            nameof(DiscountCode),
            nameof(Notification)
        ];

        private readonly IHttpContextAccessor? _httpContextAccessor;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IHttpContextAccessor? httpContextAccessor = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // 🛍️ Sản phẩm
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Category> Categories { get; set; }

        // 🛒 Giỏ hàng
        public DbSet<CartItem> CartItems { get; set; }

        // 📦 Đơn hàng
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
        
        public DbSet<SupportRequest> SupportRequests { get; set; }
        public DbSet<SupportFaq> SupportFaqs { get; set; }


        public DbSet<Review> Reviews { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<DiscountCode> DiscountCodes { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        // 📢 Thông báo
        public DbSet<Notification> Notifications { get; set; } // <-- Thêm mới

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // =========================
            // DECIMAL PRECISION
            // =========================
            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.SubtotalAmount)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.DiscountAmount)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.DiscountCode)
                .HasMaxLength(32);

            builder.Entity<Order>()
                .Property(o => o.PaymentStatus)
                .HasMaxLength(32)
                .HasDefaultValue(PaymentStatuses.Unpaid);

            builder.Entity<Order>()
                .Property(o => o.AdminNote)
                .HasMaxLength(500);

            builder.Entity<OrderStatusHistory>()
                .Property(history => history.ChangeType)
                .HasMaxLength(32);

            builder.Entity<OrderStatusHistory>()
                .Property(history => history.FromValue)
                .HasMaxLength(64);

            builder.Entity<OrderStatusHistory>()
                .Property(history => history.ToValue)
                .HasMaxLength(64);

            builder.Entity<OrderStatusHistory>()
                .Property(history => history.Note)
                .HasMaxLength(256);

            builder.Entity<OrderStatusHistory>()
                .Property(history => history.ChangedBy)
                .HasMaxLength(128);

            builder.Entity<DiscountCode>()
                .Property(code => code.Code)
                .HasMaxLength(32);

            builder.Entity<DiscountCode>()
                .HasIndex(code => code.Code)
                .IsUnique();

            builder.Entity<DiscountCode>()
                .Property(code => code.IsPublic)
                .HasDefaultValue(true);

            builder.Entity<OrderDetail>()
                .Property(od => od.Price)
                .HasPrecision(18, 2);

            builder.Entity<ProductVariant>()
                .Property(v => v.Price)
                .HasPrecision(18, 2);

            builder.Entity<ProductVariant>()
                .Property(v => v.RowVersion)
                .IsRowVersion();

            builder.Entity<CartItem>()
                .Property(item => item.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            builder.Entity<AuditLog>()
                .Property(log => log.UserId)
                .HasMaxLength(450);

            builder.Entity<AuditLog>()
                .Property(log => log.UserName)
                .HasMaxLength(256);

            builder.Entity<AuditLog>()
                .Property(log => log.Roles)
                .HasMaxLength(512);

            builder.Entity<AuditLog>()
                .Property(log => log.Action)
                .HasMaxLength(32);

            builder.Entity<AuditLog>()
                .Property(log => log.EntityName)
                .HasMaxLength(128);

            builder.Entity<AuditLog>()
                .Property(log => log.EntityId)
                .HasMaxLength(128);

            builder.Entity<AuditLog>()
                .HasIndex(log => new { log.EntityName, log.EntityId });

            builder.Entity<AuditLog>()
                .HasIndex(log => log.CreatedAt);

            // =========================
            // RELATIONSHIPS
            // =========================
            builder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderStatusHistory>()
                .HasOne(history => history.Order)
                .WithMany(order => order.StatusHistories)
                .HasForeignKey(history => history.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderDetail>()
                .HasOne(od => od.ProductVariant)
                .WithMany()
                .HasForeignKey(od => od.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
                .HasOne(review => review.Product)
                .WithMany(product => product.Reviews)
                .HasForeignKey(review => review.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Review>()
                .HasOne(review => review.User)
                .WithMany()
                .HasForeignKey(review => review.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Review>()
                .HasIndex(review => new { review.ProductId, review.UserId })
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL");

            builder.Entity<WishlistItem>()
                .Property(item => item.UserId)
                .HasMaxLength(450);

            builder.Entity<WishlistItem>()
                .HasOne(item => item.Product)
                .WithMany(product => product.WishlistItems)
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WishlistItem>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WishlistItem>()
                .HasIndex(item => new { item.UserId, item.ProductId })
                .IsUnique();

            // =========================
            // SEED DATA CHO CATEGORY
            // =========================
            builder.Entity<Category>().HasData(
                new Category { CategoryId = 1, Name = "Đồ Nam", IsActive = true },
                new Category { CategoryId = 2, Name = "Đồ Nữ", IsActive = true },
                new Category { CategoryId = 3, Name = "Bé Trai", IsActive = true },
                new Category { CategoryId = 4, Name = "Bé Gái", IsActive = true }
            );

            // =========================
            // SEED DATA CHO NOTIFICATION
            // =========================
            builder.Entity<Notification>().HasData(
                new Notification
                {
                    NotificationId = 1,
                    Title = "Flash Sale hôm nay!",
                    Content = "Giảm giá lên tới 50% cho tất cả sản phẩm.",
                    CreatedAt = new DateTime(2026, 1, 13, 10, 0, 0), // <-- cố định
                    Type = "flash",
                    IsActive = true,
                    Priority = 10
                },
                new Notification
                {
                    NotificationId = 2,
                    Title = "Miễn phí vận chuyển",
                    Content = "Áp dụng cho đơn hàng trên 500k.",
                    CreatedAt = new DateTime(2026, 1, 12, 9, 30, 0), // <-- cố định
                    Type = "promo",
                    IsActive = true,
                    Priority = 8
                },
                new Notification
                {
                    NotificationId = 3,
                    Title = "Bộ sưu tập mới",
                    Content = "Các mẫu áo mới đã có mặt trên shop.",
                    CreatedAt = new DateTime(2026, 1, 10, 14, 0, 0), // <-- cố định
                    Type = "news",
                    IsActive = true,
                    Priority = 5
                }
            );

        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var auditLogs = BuildAuditLogs();
            if (auditLogs.Any())
            {
                AuditLogs.AddRange(auditLogs);
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        private List<AuditLog> BuildAuditLogs()
        {
            ChangeTracker.DetectChanges();

            var httpContext = _httpContextAccessor?.HttpContext;
            var user = httpContext?.User;
            var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var userName = user?.Identity?.IsAuthenticated == true
                ? user.Identity.Name ?? "AuthenticatedUser"
                : "System";
            var roles = user == null
                ? string.Empty
                : string.Join(",", user.Claims
                    .Where(claim => claim.Type == ClaimTypes.Role)
                    .Select(claim => claim.Value)
                    .Distinct());

            return ChangeTracker.Entries()
                .Where(ShouldAudit)
                .Select(entry => CreateAuditLog(entry, userId, userName, roles))
                .ToList();
        }

        private static bool ShouldAudit(EntityEntry entry)
        {
            return entry.Entity is not AuditLog &&
                   entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                   AuditedEntities.Contains(entry.Metadata.ClrType.Name);
        }

        private static AuditLog CreateAuditLog(EntityEntry entry, string userId, string userName, string roles)
        {
            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                if (property.Metadata.IsPrimaryKey() || property.Metadata.IsConcurrencyToken)
                {
                    continue;
                }

                if (entry.State == EntityState.Added)
                {
                    newValues[property.Metadata.Name] = property.CurrentValue;
                }
                else if (entry.State == EntityState.Deleted)
                {
                    oldValues[property.Metadata.Name] = property.OriginalValue;
                }
                else if (property.IsModified)
                {
                    oldValues[property.Metadata.Name] = property.OriginalValue;
                    newValues[property.Metadata.Name] = property.CurrentValue;
                }
            }

            return new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Roles = roles,
                Action = entry.State.ToString(),
                EntityName = entry.Metadata.ClrType.Name,
                EntityId = GetPrimaryKeyValue(entry),
                OldValues = JsonSerializer.Serialize(oldValues),
                NewValues = JsonSerializer.Serialize(newValues),
                CreatedAt = DateTime.Now
            };
        }

        private static string GetPrimaryKeyValue(EntityEntry entry)
        {
            var primaryKey = entry.Metadata.FindPrimaryKey();
            if (primaryKey == null)
            {
                return string.Empty;
            }

            return string.Join(",", primaryKey.Properties
                .Select(property => entry.Property(property.Name).CurrentValue?.ToString() ?? string.Empty));
        }
    }
}
