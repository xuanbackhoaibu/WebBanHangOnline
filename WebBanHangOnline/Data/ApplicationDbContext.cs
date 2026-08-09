using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
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
        private static readonly HashSet<Type> AuditedEntityTypes =
        [
            typeof(Product),
            typeof(ProductVariant),
            typeof(Category),
            typeof(Order),
            typeof(OrderDetail),
            typeof(DiscountCode),
            typeof(Notification),
            typeof(ApplicationUser),
            typeof(IdentityUserRole<string>),
            typeof(SupportRequest),
            typeof(SupportFaq),
            typeof(Review)
        ];

        private readonly IHttpContextAccessor? _httpContextAccessor;
        private bool _isSavingAuditLogs;

        public bool AuditEnabled { get; set; } = true;

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

            builder.Entity<DiscountCode>()
                .Property(code => code.RowVersion)
                .IsRowVersion();

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

        public override int SaveChanges()
        {
            return SaveChanges(true);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            if (_isSavingAuditLogs)
            {
                return base.SaveChanges(acceptAllChangesOnSuccess);
            }

            var auditEntries = BuildAuditEntries();
            var result = base.SaveChanges(acceptAllChangesOnSuccess);
            SaveAuditLogs(auditEntries);

            return result;
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return SaveChangesAsync(true, cancellationToken);
        }

        public override async Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            if (_isSavingAuditLogs)
            {
                return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            }

            var auditEntries = BuildAuditEntries();
            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            await SaveAuditLogsAsync(auditEntries, cancellationToken);

            return result;
        }

        private List<PendingAuditEntry> BuildAuditEntries()
        {
            if (!AuditEnabled)
            {
                return [];
            }

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
                .Select(entry => CreatePendingAuditEntry(entry, userId, userName, roles))
                .ToList();
        }

        private static bool ShouldAudit(EntityEntry entry)
        {
            return entry.Entity is not AuditLog &&
                   entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                   AuditedEntityTypes.Contains(entry.Metadata.ClrType);
        }

        private static PendingAuditEntry CreatePendingAuditEntry(EntityEntry entry, string userId, string userName, string roles)
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

            return new PendingAuditEntry(
                entry,
                userId,
                userName,
                roles,
                entry.State.ToString(),
                entry.Metadata.ClrType.Name,
                oldValues,
                newValues);
        }

        private void SaveAuditLogs(IReadOnlyCollection<PendingAuditEntry> auditEntries)
        {
            if (auditEntries.Count == 0)
            {
                return;
            }

            _isSavingAuditLogs = true;
            try
            {
                AuditLogs.AddRange(auditEntries.Select(CreateAuditLog));
                base.SaveChanges();
            }
            finally
            {
                _isSavingAuditLogs = false;
            }
        }

        private async Task SaveAuditLogsAsync(
            IReadOnlyCollection<PendingAuditEntry> auditEntries,
            CancellationToken cancellationToken)
        {
            if (auditEntries.Count == 0)
            {
                return;
            }

            _isSavingAuditLogs = true;
            try
            {
                await AuditLogs.AddRangeAsync(auditEntries.Select(CreateAuditLog), cancellationToken);
                await base.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                _isSavingAuditLogs = false;
            }
        }

        private static AuditLog CreateAuditLog(PendingAuditEntry auditEntry)
        {
            return new AuditLog
            {
                UserId = auditEntry.UserId,
                UserName = auditEntry.UserName,
                Roles = auditEntry.Roles,
                Action = auditEntry.Action,
                EntityName = auditEntry.EntityName,
                EntityId = GetPrimaryKeyValue(auditEntry.Entry),
                OldValues = JsonSerializer.Serialize(auditEntry.OldValues),
                NewValues = JsonSerializer.Serialize(auditEntry.NewValues),
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

        private sealed record PendingAuditEntry(
            EntityEntry Entry,
            string UserId,
            string UserName,
            string Roles,
            string Action,
            string EntityName,
            IReadOnlyDictionary<string, object?> OldValues,
            IReadOnlyDictionary<string, object?> NewValues);
    }
}
