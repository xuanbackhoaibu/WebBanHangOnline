using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Models;

namespace WebBanHangOnline.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            // ============================
            // LẤY SERVICE (KHÔNG TẠO SCOPE MỚI)
            // ============================
            var context = services.GetRequiredService<ApplicationDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // ============================
            // 1️⃣ MIGRATE DATABASE
            // ============================
            await context.Database.MigrateAsync();

            // ============================
            // 2️⃣ SEED ROLES (ADMIN + USER)
            // ============================
            string[] roles = { "Admin", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // ============================
            // 3️⃣ SEED ADMIN USER
            // ============================
            var adminEmail = "admin@shop.com";
            var adminPassword = "Admin@123";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Administrator"
                };

                await userManager.CreateAsync(adminUser, adminPassword);
            }

            // GÁN ROLE ADMIN (PHÒNG TRƯỜNG HỢP CHƯA CÓ)
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // ============================
            // 4️⃣ SEED CATEGORY (WEB BÁN HÀNG)
            // ============================
            if (!await context.Categories.AnyAsync())
            {
                context.Categories.AddRange(
                    new Category { Name = "Đồ Nam", IsActive = true },
                    new Category { Name = "Đồ Nữ", IsActive = true },
                    new Category { Name = "Bé Trai", IsActive = true },
                    new Category { Name = "Bé Gái", IsActive = true }
                );

                await context.SaveChangesAsync();
            }
        }
    }
}
