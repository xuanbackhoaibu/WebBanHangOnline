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
            // 2️⃣ SEED ROLES (ADMIN + USER + CLIEND)
            // ============================
            string[] roles = { "Admin", "User", "Client" };

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

            await SeedFashionStoreDataAsync(context, userManager);
        }

        private static async Task SeedFashionStoreDataAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            await EnsureFashionColumnsAsync(context);

            if (!await context.SupportFaqs.AnyAsync())
            {
                context.SupportFaqs.AddRange(
                    new SupportFaq
                    {
                        Question = "Shop có hỗ trợ đổi size không?",
                        Answer = "Có. Khách hàng có thể đổi size trong 7 ngày nếu sản phẩm còn nguyên tem mác và chưa qua sử dụng.",
                        IsActive = true
                    },
                    new SupportFaq
                    {
                        Question = "Thời gian giao hàng là bao lâu?",
                        Answer = "Nội thành thường từ 1-2 ngày, các tỉnh từ 3-5 ngày tùy đơn vị vận chuyển.",
                        IsActive = true
                    },
                    new SupportFaq
                    {
                        Question = "Shop hỗ trợ những phương thức thanh toán nào?",
                        Answer = "Shop hỗ trợ COD, VNPay, MoMo và VietQR. Khi chạy demo Docker, các cổng thanh toán để ở chế độ cấu hình mẫu.",
                        IsActive = true
                    }
                );
            }

            if (!await context.Products.AnyAsync())
            {
                var categories = await context.Categories.ToDictionaryAsync(c => c.Name, c => c.CategoryId);
                var now = DateTime.Now;

                var products = new List<Product>
                {
                    CreateProduct("Áo sơ mi nam Oxford trắng", categories["Đồ Nam"], 349000, 279000, "/images/products/e8601b06-4931-4947-bb9d-88c100249e8c.webp", "Áo sơ mi Oxford form regular, dễ phối đi học, đi làm và gặp khách hàng.", now),
                    CreateProduct("Áo polo nam pique xanh navy", categories["Đồ Nam"], 299000, 239000, "/images/products/c585b184-79a3-40f5-9d35-8e5bdd9db19e.webp", "Polo chất pique thoáng, cổ đứng form gọn, phù hợp phong cách smart casual.", now),
                    CreateProduct("Quần kaki nam slimfit be", categories["Đồ Nam"], 420000, null, "/images/products/3e322809-1097-468a-8d36-c1c118ca9952.webp", "Quần kaki co giãn nhẹ, dáng slimfit lịch sự cho công sở và đi chơi.", now),
                    CreateProduct("Áo thun basic nam cotton", categories["Đồ Nam"], 199000, 159000, "/images/products/c3bc6230-25f9-4637-8659-7d824922f2ef.webp", "Áo thun cotton mềm, màu trung tính, dễ phối với jeans hoặc kaki.", now),
                    CreateProduct("Đầm midi nữ hoa nhí", categories["Đồ Nữ"], 459000, 369000, "/images/products/c40ccd42-a351-4eb2-9a7a-2204c7fd7ee5.webp", "Đầm midi nhẹ nhàng, họa tiết hoa nhí, phù hợp đi làm và dạo phố.", now),
                    CreateProduct("Blazer nữ form ngắn", categories["Đồ Nữ"], 699000, null, "/images/products/4860faca-390b-4f50-85aa-01399150c6f4.jfif", "Blazer form ngắn, chất đứng dáng, phối tốt với chân váy hoặc quần tây.", now),
                    CreateProduct("Chân váy chữ A đen", categories["Đồ Nữ"], 329000, 269000, "/images/products/ebfc157b-4fa8-47c1-8437-32849bdc462a.webp", "Chân váy chữ A basic, dễ mặc, phù hợp phong cách tối giản.", now),
                    CreateProduct("Áo kiểu nữ tay phồng", categories["Đồ Nữ"], 289000, null, "/images/products/cbfde535-3951-4782-88d1-e490867ecfcc.webp", "Áo kiểu nữ tay phồng nhẹ, tạo điểm nhấn mềm mại cho trang phục hằng ngày.", now),
                    CreateProduct("Set bé trai áo thun quần short", categories["Bé Trai"], 259000, 219000, "/images/products/d083a76c-624d-49f6-8be9-b88de12f6621.webp", "Set đồ năng động cho bé trai, chất liệu cotton thoáng mát.", now),
                    CreateProduct("Áo khoác bé trai thể thao", categories["Bé Trai"], 319000, null, "/images/products/fde50f89-2adc-4c27-ad63-760bd01ff200.webp", "Áo khoác nhẹ cho bé trai, phù hợp đi học và hoạt động ngoài trời.", now),
                    CreateProduct("Váy công chúa bé gái pastel", categories["Bé Gái"], 379000, 309000, "/images/products/918b81e6-ea30-4b0d-abad-d5070f8cd55d.webp", "Váy pastel dễ thương cho bé gái, phù hợp sinh nhật và dịp đặc biệt.", now),
                    CreateProduct("Set bé gái áo blouse chân váy", categories["Bé Gái"], 299000, null, "/images/products/534f4fe9-264f-4fd6-85cb-506ab2c3327b.webp", "Set phối sẵn gọn gàng, màu sáng, phù hợp đi học và đi chơi cuối tuần.", now)
                };

                foreach (var product in products)
                {
                    product.GenerateSlug();
                    product.ImageUrl = product.Thumbnail;
                    product.Images.Add(new ProductImage { ImageUrl = product.Thumbnail });
                    product.Images.Add(new ProductImage { ImageUrl = product.Thumbnail });
                    product.Variants.Add(new ProductVariant { Size = "S", Color = "Trắng", Stock = 18, Price = product.FinalPrice });
                    product.Variants.Add(new ProductVariant { Size = "M", Color = "Đen", Stock = 24, Price = product.FinalPrice });
                    product.Variants.Add(new ProductVariant { Size = "L", Color = "Navy", Stock = 16, Price = product.FinalPrice });
                }

                context.Products.AddRange(products);
                await context.SaveChangesAsync();
            }

            await SeedDemoCustomerAndOrderAsync(context, userManager);
        }

        private static async Task EnsureFashionColumnsAsync(ApplicationDbContext context)
        {
            await context.Database.ExecuteSqlRawAsync(@"
IF COL_LENGTH('Products', 'FlashSalePrice') IS NULL
    ALTER TABLE Products ADD FlashSalePrice decimal(18,2) NULL;

IF COL_LENGTH('Products', 'FlashSaleStart') IS NULL
    ALTER TABLE Products ADD FlashSaleStart datetime2 NULL;

IF COL_LENGTH('Products', 'FlashSaleEnd') IS NULL
    ALTER TABLE Products ADD FlashSaleEnd datetime2 NULL;
");
        }

        private static Product CreateProduct(string name, int categoryId, decimal price, decimal? salePrice, string image, string description, DateTime now)
        {
            return new Product
            {
                Name = name,
                CategoryId = categoryId,
                Description = description,
                Price = price,
                FlashSalePrice = salePrice,
                FlashSaleStart = salePrice.HasValue ? now.AddDays(-2) : null,
                FlashSaleEnd = salePrice.HasValue ? now.AddDays(14) : null,
                Thumbnail = image,
                ImageUrl = image,
                IsActive = true
            };
        }

        private static async Task SeedDemoCustomerAndOrderAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            var customerEmail = "customer@shop.com";
            var customer = await userManager.FindByEmailAsync(customerEmail);

            if (customer == null)
            {
                customer = new ApplicationUser
                {
                    UserName = customerEmail,
                    Email = customerEmail,
                    EmailConfirmed = true,
                    FullName = "Khách hàng Demo",
                    PhoneNumber = "0900000001",
                    Province = "Hà Nội",
                    District = "Cầu Giấy",
                    Ward = "Dịch Vọng",
                    StreetAddress = "Số 1 Xuân Thủy"
                };

                await userManager.CreateAsync(customer, "Customer@123");
                await userManager.AddToRoleAsync(customer, "Client");
            }

            if (!await context.Orders.AnyAsync())
            {
                var variants = await context.ProductVariants
                    .OrderBy(v => v.Id)
                    .Take(2)
                    .ToListAsync();

                if (variants.Count == 0)
                {
                    return;
                }

                var order = new Order
                {
                    UserId = customer.Id,
                    OrderDate = DateTime.Now.AddDays(-1),
                    ShippingAddress = "Số 1 Xuân Thủy, Dịch Vọng, Cầu Giấy, Hà Nội",
                    PhoneNumber = "0900000001",
                    Status = "Completed",
                    PaymentMethod = "COD",
                    PaymentDate = DateTime.Now,
                    OrderDetails = variants.Select(variant => new OrderDetail
                    {
                        ProductVariantId = variant.Id,
                        Quantity = 1,
                        Price = variant.Price
                    }).ToList()
                };

                order.TotalAmount = order.OrderDetails.Sum(item => item.Price * item.Quantity);
                context.Orders.Add(order);
                await context.SaveChangesAsync();
            }
        }
    }
}
