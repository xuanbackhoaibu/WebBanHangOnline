using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanHangOnline.Migrations
{
    [Migration("20260805130000_AddDiscountCodes")]
    public partial class AddDiscountCodes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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

EXEC(N'
UPDATE [Orders]
SET [SubtotalAmount] = [TotalAmount]
WHERE [SubtotalAmount] = 0;
');

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Orders', 'DiscountCode') IS NOT NULL
    ALTER TABLE [Orders] DROP COLUMN [DiscountCode];

IF COL_LENGTH('Orders', 'DiscountAmount') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Orders_DiscountAmount')
        ALTER TABLE [Orders] DROP CONSTRAINT [DF_Orders_DiscountAmount];
    ALTER TABLE [Orders] DROP COLUMN [DiscountAmount];
END

IF COL_LENGTH('Orders', 'SubtotalAmount') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Orders_SubtotalAmount')
        ALTER TABLE [Orders] DROP CONSTRAINT [DF_Orders_SubtotalAmount];
    ALTER TABLE [Orders] DROP COLUMN [SubtotalAmount];
END

IF OBJECT_ID(N'[DiscountCodes]', N'U') IS NOT NULL
    DROP TABLE [DiscountCodes];
");
        }
    }
}
