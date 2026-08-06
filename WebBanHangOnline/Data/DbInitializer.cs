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
            await EnsureOrderPaymentStatusAsync(context);
            await EnsureReviewTableAsync(context);
            await EnsureWishlistTableAsync(context);
            await EnsureDiscountCodesAsync(context);
            await EnsureOrderAdminNotesAndHistoriesAsync(context);

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

            await SeedCatalogProductsAsync(context);
            await SeedProductReviewsAsync(context);

            await SeedDemoCustomerAndOrderAsync(context, userManager);
        }

        private static async Task SeedCatalogProductsAsync(ApplicationDbContext context)
        {
            var categories = await context.Categories.ToDictionaryAsync(c => c.Name, c => c.CategoryId);
            var now = DateTime.Now;
            var seeds = new List<ProductSeed>
            {
                new("Áo sơ mi nam Oxford trắng", "Đồ Nam", 349000, 279000, "/images/products/e8601b06-4931-4947-bb9d-88c100249e8c.webp", "Áo sơ mi Oxford form regular, dễ phối đi học, đi làm và gặp khách hàng.", new[] { "Trắng", "Xanh", "Đen" }),
                new("Áo polo nam pique xanh navy", "Đồ Nam", 299000, 239000, "/images/products/c585b184-79a3-40f5-9d35-8e5bdd9db19e.webp", "Polo chất pique thoáng, cổ đứng form gọn, phù hợp phong cách smart casual.", new[] { "Navy", "Trắng", "Đen" }),
                new("Quần kaki nam slimfit be", "Đồ Nam", 420000, null, "/images/products/3e322809-1097-468a-8d36-c1c118ca9952.webp", "Quần kaki co giãn nhẹ, dáng slimfit lịch sự cho công sở và đi chơi.", new[] { "Be", "Đen", "Nâu" }),
                new("Áo thun basic nam cotton", "Đồ Nam", 199000, 159000, "/images/products/c3bc6230-25f9-4637-8659-7d824922f2ef.webp", "Áo thun cotton mềm, màu trung tính, dễ phối với jeans hoặc kaki.", new[] { "Trắng", "Đen", "Xám" }),
                new("Quần jeans nam xanh straight", "Đồ Nam", 459000, 389000, "/images/products/557530f1-06eb-4210-ac21-04b7b57537a1.webp", "Jeans dáng straight thoải mái, màu xanh dễ mặc cho nhiều dáng người.", new[] { "Xanh", "Đen", "Xám" }),
                new("Áo khoác bomber nam", "Đồ Nam", 549000, null, "/images/products/4fe431fd-d51e-43cf-8253-5fd5f1bf259f.webp", "Bomber nhẹ, bo cổ tay gọn, hợp đi học, đi làm và dạo phố.", new[] { "Đen", "Xanh", "Be" }),
                new("Hoodie nam nỉ mềm", "Đồ Nam", 399000, 329000, "/images/products/b916f893-5a5e-4398-8003-c853f4db4dff.jfif", "Hoodie nỉ mềm, form rộng vừa, giữ ấm tốt nhưng vẫn thoáng.", new[] { "Xám", "Đen", "Trắng" }),
                new("Quần short nam thể thao", "Đồ Nam", 249000, null, "/images/products/868b37fa-f866-4fc4-bf05-1a6914c3f3c4.jfif", "Short thể thao co giãn, có túi tiện dụng cho vận động hằng ngày.", new[] { "Đen", "Xanh", "Xám" }),

                new("Đầm midi nữ hoa nhí", "Đồ Nữ", 459000, 369000, "/images/products/c40ccd42-a351-4eb2-9a7a-2204c7fd7ee5.webp", "Đầm midi nhẹ nhàng, họa tiết hoa nhí, phù hợp đi làm và dạo phố.", new[] { "Trắng", "Hồng", "Xanh" }),
                new("Blazer nữ form ngắn", "Đồ Nữ", 699000, null, "/images/products/4860faca-390b-4f50-85aa-01399150c6f4.jfif", "Blazer form ngắn, chất đứng dáng, phối tốt với chân váy hoặc quần tây.", new[] { "Đen", "Be", "Trắng" }),
                new("Chân váy chữ A đen", "Đồ Nữ", 329000, 269000, "/images/products/ebfc157b-4fa8-47c1-8437-32849bdc462a.webp", "Chân váy chữ A basic, dễ mặc, phù hợp phong cách tối giản.", new[] { "Đen", "Nâu", "Be" }),
                new("Áo kiểu nữ tay phồng", "Đồ Nữ", 289000, null, "/images/products/cbfde535-3951-4782-88d1-e490867ecfcc.webp", "Áo kiểu nữ tay phồng nhẹ, tạo điểm nhấn mềm mại cho trang phục hằng ngày.", new[] { "Trắng", "Hồng", "Xanh" }),
                new("Quần tây nữ ống suông", "Đồ Nữ", 399000, 329000, "/images/products/78cdc69a-fee0-4715-96d5-af8b85028682.webp", "Quần tây ống suông đứng dáng, phù hợp công sở và gặp khách hàng.", new[] { "Đen", "Be", "Xám" }),
                new("Áo cardigan nữ len mỏng", "Đồ Nữ", 359000, null, "/images/products/1201be9b-2a0d-46b8-ab41-662740f8ca4d.webp", "Cardigan len mỏng mềm, dễ khoác ngoài váy hoặc áo thun.", new[] { "Hồng", "Trắng", "Be" }),
                new("Set công sở nữ thanh lịch", "Đồ Nữ", 759000, 649000, "/images/products/df26599a-7ad1-40fa-97f2-89a23028b78c.jfif", "Set áo và quần phối sẵn, gọn gàng cho phong cách công sở hiện đại.", new[] { "Trắng", "Đen", "Be" }),
                new("Áo thun nữ cổ tròn", "Đồ Nữ", 189000, null, "/images/products/e50edb29-737c-4f6a-ae5b-bc5b084bef0e.webp", "Áo thun nữ cổ tròn, chất cotton mềm, hợp mặc hằng ngày.", new[] { "Trắng", "Đen", "Hồng" }),

                new("Set bé trai áo thun quần short", "Bé Trai", 259000, 219000, "/images/products/d083a76c-624d-49f6-8be9-b88de12f6621.webp", "Set đồ năng động cho bé trai, chất liệu cotton thoáng mát.", new[] { "Xanh", "Trắng", "Đen" }),
                new("Áo khoác bé trai thể thao", "Bé Trai", 319000, null, "/images/products/fde50f89-2adc-4c27-ad63-760bd01ff200.webp", "Áo khoác nhẹ cho bé trai, phù hợp đi học và hoạt động ngoài trời.", new[] { "Xanh", "Đen", "Đỏ" }),
                new("Áo polo bé trai đi học", "Bé Trai", 229000, 189000, "/images/products/910c41de-26e6-4113-9a3c-a90f4fde9229.webp", "Polo cotton lịch sự, dễ giặt, phù hợp đi học và đi chơi.", new[] { "Trắng", "Xanh", "Đỏ" }),
                new("Quần jogger bé trai", "Bé Trai", 239000, null, "/images/products/758fbf3d-6af4-41b5-b13b-0f807c3ecb5d.webp", "Jogger co giãn nhẹ, bo ống gọn, bé vận động thoải mái.", new[] { "Đen", "Xám", "Xanh" }),
                new("Áo sơ mi bé trai caro", "Bé Trai", 269000, 219000, "/images/products/ca67c43c-adcd-403d-86d5-11dac122de84.webp", "Sơ mi caro mềm, dễ phối cùng quần jeans hoặc short.", new[] { "Xanh", "Đỏ", "Trắng" }),
                new("Bộ đồ thể thao bé trai", "Bé Trai", 349000, null, "/images/products/0b96e441-85a4-4775-a77c-9542ef56a37b.webp", "Bộ thể thao thoáng, phù hợp đi học thể chất và hoạt động cuối tuần.", new[] { "Đen", "Xanh", "Xám" }),

                new("Váy công chúa bé gái pastel", "Bé Gái", 379000, 309000, "/images/products/918b81e6-ea30-4b0d-abad-d5070f8cd55d.webp", "Váy pastel dễ thương cho bé gái, phù hợp sinh nhật và dịp đặc biệt.", new[] { "Hồng", "Trắng", "Xanh" }),
                new("Set bé gái áo blouse chân váy", "Bé Gái", 299000, null, "/images/products/534f4fe9-264f-4fd6-85cb-506ab2c3327b.webp", "Set phối sẵn gọn gàng, màu sáng, phù hợp đi học và đi chơi cuối tuần.", new[] { "Trắng", "Hồng", "Be" }),
                new("Đầm denim bé gái", "Bé Gái", 329000, 279000, "/images/products/8554361c-05b6-48ed-bc79-229c35fa751d.webp", "Đầm denim mềm, kiểu dáng năng động, dễ phối giày sneaker.", new[] { "Xanh", "Trắng", "Hồng" }),
                new("Áo cardigan bé gái", "Bé Gái", 249000, null, "/images/products/4c3e37b8-86d9-4586-a95e-baa03eed81f4.webp", "Cardigan mỏng giữ ấm nhẹ, phù hợp khoác ngoài váy hoặc áo thun.", new[] { "Hồng", "Trắng", "Be" }),
                new("Áo thun bé gái in hình", "Bé Gái", 179000, 149000, "/images/products/cf9a2c09-8773-4ea9-8971-4eefd5aecf9c.webp", "Áo thun mềm, họa tiết dễ thương, bé mặc thoải mái cả ngày.", new[] { "Trắng", "Hồng", "Xanh" }),
                new("Chân váy tutu bé gái", "Bé Gái", 259000, null, "/images/products/6e191c5f-b846-4d38-8176-3b17c74d713b.webp", "Chân váy tutu bồng nhẹ, hợp tiệc nhỏ và chụp ảnh.", new[] { "Hồng", "Trắng", "Tím" })
            };

            var seedNames = seeds.Select(seed => seed.Name).ToList();
            var existingProducts = await context.Products
                .Include(product => product.Images)
                .Include(product => product.Variants)
                .Where(product => seedNames.Contains(product.Name))
                .ToDictionaryAsync(product => product.Name);

            foreach (var seed in seeds)
            {
                var categoryId = categories[seed.CategoryName];

                if (!existingProducts.TryGetValue(seed.Name, out var product))
                {
                    product = CreateProduct(seed.Name, categoryId, seed.Price, seed.SalePrice, seed.Image, seed.Description, now);
                    product.GenerateSlug();
                    product.Images.Add(new ProductImage { ImageUrl = product.Thumbnail });
                    product.Images.Add(new ProductImage { ImageUrl = product.Thumbnail });
                    context.Products.Add(product);
                }
                else
                {
                    product.CategoryId = categoryId;
                    product.IsActive = true;
                    product.Description = seed.Description;
                    product.Price = seed.Price;
                    product.FlashSalePrice = seed.SalePrice;
                    product.FlashSaleStart = seed.SalePrice.HasValue ? now.AddDays(-2) : null;
                    product.FlashSaleEnd = seed.SalePrice.HasValue ? now.AddDays(14) : null;
                    product.Thumbnail = seed.Image;
                    product.ImageUrl = seed.Image;

                    if (string.IsNullOrWhiteSpace(product.Slug))
                    {
                        product.GenerateSlug();
                    }

                    if (!product.Images.Any(image => image.ImageUrl == seed.Image))
                    {
                        product.Images.Add(new ProductImage { ImageUrl = seed.Image });
                    }
                }

                AddMissingVariants(product, seed.Colors);
            }

            await context.SaveChangesAsync();
        }

        private static void AddMissingVariants(Product product, IEnumerable<string> colors)
        {
            var sizes = new[] { "S", "M", "L", "XL" };
            var stockBySize = new Dictionary<string, int>
            {
                ["S"] = 18,
                ["M"] = 24,
                ["L"] = 20,
                ["XL"] = 14
            };

            foreach (var color in colors)
            {
                foreach (var size in sizes)
                {
                    if (product.Variants.Any(variant => variant.Size == size && variant.Color == color))
                    {
                        continue;
                    }

                    product.Variants.Add(new ProductVariant
                    {
                        Size = size,
                        Color = color,
                        Stock = stockBySize[size],
                        Price = product.FinalPrice
                    });
                }
            }
        }

        private sealed record ProductSeed(
            string Name,
            string CategoryName,
            decimal Price,
            decimal? SalePrice,
            string Image,
            string Description,
            IReadOnlyCollection<string> Colors);

        private static async Task SeedProductReviewsAsync(ApplicationDbContext context)
        {
            if (await context.Reviews.AnyAsync())
            {
                return;
            }

            var products = await context.Products
                .OrderBy(product => product.ProductId)
                .Take(8)
                .ToListAsync();

            var reviewTemplates = new[]
            {
                new { UserName = "Minh Anh", Rating = 5, Comment = "Sản phẩm đẹp, chất vải mềm và form đúng mô tả." },
                new { UserName = "Hoàng Nam", Rating = 5, Comment = "Giao nhanh, đóng gói cẩn thận, mặc rất vừa." },
                new { UserName = "Thu Hà", Rating = 4, Comment = "Màu đẹp, đường may ổn, sẽ ủng hộ shop tiếp." }
            };

            foreach (var product in products)
            {
                foreach (var template in reviewTemplates)
                {
                    context.Reviews.Add(new Review
                    {
                        ProductId = product.ProductId,
                        UserName = template.UserName,
                        Rating = template.Rating,
                        Comment = template.Comment,
                        CreatedAt = DateTime.Now.AddDays(-template.Rating)
                    });
                }
            }

            await context.SaveChangesAsync();
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

        private static async Task EnsureOrderPaymentStatusAsync(ApplicationDbContext context)
        {
            await context.Database.ExecuteSqlRawAsync(@"
IF COL_LENGTH('Orders', 'PaymentStatus') IS NULL
    ALTER TABLE [Orders] ADD [PaymentStatus] nvarchar(32) NOT NULL CONSTRAINT [DF_Orders_PaymentStatus] DEFAULT N'Unpaid';

EXEC(N'
UPDATE [Orders]
SET [PaymentStatus] =
    CASE
        WHEN [Status] = N''Paid'' THEN N''Paid''
        WHEN [Status] = N''Failed'' THEN N''Failed''
        WHEN [Status] = N''Refunded'' THEN N''Refunded''
        WHEN [PaymentMethod] = N''COD'' AND [Status] IN (N''Confirmed'', N''Completed'') THEN N''Paid''
        ELSE [PaymentStatus]
    END;
');

UPDATE [Orders]
SET [Status] = N'Confirmed'
WHERE [Status] IN (N'Paid', N'Failed', N'Refunded');
");
        }

        private static async Task EnsureReviewTableAsync(ApplicationDbContext context)
        {
            await context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[Reviews]', N'U') IS NULL
BEGIN
    CREATE TABLE [Reviews] (
        [ReviewId] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [UserId] nvarchar(450) NULL,
        [UserName] nvarchar(max) NOT NULL,
        [Rating] int NOT NULL,
        [Comment] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Reviews] PRIMARY KEY ([ReviewId]),
        CONSTRAINT [FK_Reviews_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([ProductId]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_Reviews_ProductId] ON [Reviews] ([ProductId]);
    EXEC(N'CREATE INDEX [IX_Reviews_UserId] ON [Reviews] ([UserId])');
    EXEC(N'CREATE UNIQUE INDEX [IX_Reviews_ProductId_UserId] ON [Reviews] ([ProductId], [UserId]) WHERE [UserId] IS NOT NULL');
END

IF COL_LENGTH('Reviews', 'UserId') IS NULL
    ALTER TABLE [Reviews] ADD [UserId] nvarchar(450) NULL;

IF COL_LENGTH('Reviews', 'UpdatedAt') IS NULL
    ALTER TABLE [Reviews] ADD [UpdatedAt] datetime2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reviews_UserId' AND object_id = OBJECT_ID(N'[Reviews]'))
    EXEC(N'CREATE INDEX [IX_Reviews_UserId] ON [Reviews] ([UserId])');

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reviews_ProductId_UserId' AND object_id = OBJECT_ID(N'[Reviews]'))
    EXEC(N'CREATE UNIQUE INDEX [IX_Reviews_ProductId_UserId] ON [Reviews] ([ProductId], [UserId]) WHERE [UserId] IS NOT NULL');
");
        }

        private static async Task EnsureWishlistTableAsync(ApplicationDbContext context)
        {
            await context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[WishlistItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [WishlistItems] (
        [WishlistItemId] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ProductId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_WishlistItems] PRIMARY KEY ([WishlistItemId]),
        CONSTRAINT [FK_WishlistItems_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WishlistItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([ProductId]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_WishlistItems_ProductId] ON [WishlistItems] ([ProductId]);
    CREATE UNIQUE INDEX [IX_WishlistItems_UserId_ProductId] ON [WishlistItems] ([UserId], [ProductId]);
END

IF COL_LENGTH('WishlistItems', 'UserId') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WishlistItems_UserId_ProductId' AND object_id = OBJECT_ID(N'[WishlistItems]'))
        DROP INDEX [IX_WishlistItems_UserId_ProductId] ON [WishlistItems];

    ALTER TABLE [WishlistItems] ALTER COLUMN [UserId] nvarchar(450) NOT NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WishlistItems_ProductId' AND object_id = OBJECT_ID(N'[WishlistItems]'))
    CREATE INDEX [IX_WishlistItems_ProductId] ON [WishlistItems] ([ProductId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WishlistItems_UserId_ProductId' AND object_id = OBJECT_ID(N'[WishlistItems]'))
    CREATE UNIQUE INDEX [IX_WishlistItems_UserId_ProductId] ON [WishlistItems] ([UserId], [ProductId]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_WishlistItems_AspNetUsers_UserId' AND parent_object_id = OBJECT_ID(N'[WishlistItems]'))
    ALTER TABLE [WishlistItems] ADD CONSTRAINT [FK_WishlistItems_AspNetUsers_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_WishlistItems_Products_ProductId' AND parent_object_id = OBJECT_ID(N'[WishlistItems]'))
    ALTER TABLE [WishlistItems] ADD CONSTRAINT [FK_WishlistItems_Products_ProductId]
        FOREIGN KEY ([ProductId]) REFERENCES [Products] ([ProductId]) ON DELETE CASCADE;
");
        }

        private static async Task EnsureDiscountCodesAsync(ApplicationDbContext context)
        {
            await context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[DiscountCodes]', N'U') IS NULL
BEGIN
    CREATE TABLE [DiscountCodes] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(32) NOT NULL,
        [Description] nvarchar(160) NOT NULL,
        [DiscountType] nvarchar(16) NOT NULL,
        [DiscountValue] decimal(18,2) NOT NULL,
        [MinimumOrderAmount] decimal(18,2) NOT NULL,
        [MaximumDiscountAmount] decimal(18,2) NULL,
        [UsageLimit] int NULL,
        [UsedCount] int NOT NULL,
        [StartsAt] datetime2 NULL,
        [EndsAt] datetime2 NULL,
        [IsPublic] bit NOT NULL DEFAULT 1,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_DiscountCodes] PRIMARY KEY ([Id])
    );

    CREATE UNIQUE INDEX [IX_DiscountCodes_Code] ON [DiscountCodes] ([Code]);
END

IF COL_LENGTH('Orders', 'SubtotalAmount') IS NULL
    ALTER TABLE [Orders] ADD [SubtotalAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_Orders_SubtotalAmount] DEFAULT 0;

IF COL_LENGTH('Orders', 'DiscountAmount') IS NULL
    ALTER TABLE [Orders] ADD [DiscountAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_Orders_DiscountAmount] DEFAULT 0;

IF COL_LENGTH('Orders', 'DiscountCode') IS NULL
    ALTER TABLE [Orders] ADD [DiscountCode] nvarchar(32) NULL;

IF COL_LENGTH('DiscountCodes', 'IsPublic') IS NULL
    ALTER TABLE [DiscountCodes] ADD [IsPublic] bit NOT NULL CONSTRAINT [DF_DiscountCodes_IsPublic] DEFAULT 1;

EXEC(N'UPDATE [Orders] SET [SubtotalAmount] = [TotalAmount] WHERE [SubtotalAmount] = 0;');

IF NOT EXISTS (SELECT 1 FROM [DiscountCodes] WHERE [Code] = N'WELCOME10')
BEGIN
    EXEC(N'
    INSERT INTO [DiscountCodes]
        ([Code], [Description], [DiscountType], [DiscountValue], [MinimumOrderAmount], [MaximumDiscountAmount],
         [UsageLimit], [UsedCount], [StartsAt], [EndsAt], [IsPublic], [IsActive], [CreatedAt])
    VALUES
        (N''WELCOME10'', N''Giảm 10% cho đơn từ 200.000 VND'', N''Percent'', 10, 200000, 50000,
         100, 0, NULL, NULL, 1, 1, SYSUTCDATETIME());
    ');
END
");
        }

        private static async Task EnsureOrderAdminNotesAndHistoriesAsync(ApplicationDbContext context)
        {
            await context.Database.ExecuteSqlRawAsync(@"
IF COL_LENGTH('Orders', 'AdminNote') IS NULL
    ALTER TABLE [Orders] ADD [AdminNote] nvarchar(500) NULL;

IF OBJECT_ID(N'[OrderStatusHistories]', N'U') IS NULL
BEGIN
    CREATE TABLE [OrderStatusHistories] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [ChangeType] nvarchar(32) NOT NULL,
        [FromValue] nvarchar(64) NULL,
        [ToValue] nvarchar(64) NULL,
        [Note] nvarchar(256) NULL,
        [ChangedBy] nvarchar(128) NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_OrderStatusHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderStatusHistories_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_OrderStatusHistories_OrderId'
      AND [object_id] = OBJECT_ID(N'[OrderStatusHistories]')
)
    CREATE INDEX [IX_OrderStatusHistories_OrderId] ON [OrderStatusHistories] ([OrderId]);
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
                    PaymentStatus = PaymentStatuses.Paid,
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
