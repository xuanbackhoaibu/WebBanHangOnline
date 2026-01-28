using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Models;

namespace WebBanHangOnline.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // ============================
            // 1️⃣ MIGRATE DATABASE
            // ============================
            await context.Database.MigrateAsync();

            // ============================
            // 2️⃣ SEED ROLE ADMIN
            // ============================
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // ============================
            // 3️⃣ SEED USER ADMIN
            // ============================
            var adminEmail = "admin@shop.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(adminUser, "Admin@123");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // ============================
            // 4️⃣ SEED CATEGORY (QUAN TRỌNG)
            // ============================
            if (!context.Categories.Any())
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
