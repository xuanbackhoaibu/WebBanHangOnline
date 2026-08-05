using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanHangOnline.Migrations
{
    [Migration("20260805090000_AddWishlistItemsTable")]
    public partial class AddWishlistItemsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[WishlistItems]', N'U') IS NOT NULL
    DROP TABLE [WishlistItems];
");
        }
    }
}
