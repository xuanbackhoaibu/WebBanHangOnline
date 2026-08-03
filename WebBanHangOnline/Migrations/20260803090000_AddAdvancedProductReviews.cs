using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanHangOnline.Migrations
{
    /// <inheritdoc />
    [Migration("20260803090000_AddAdvancedProductReviews")]
    public partial class AddAdvancedProductReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Reviews', 'UpdatedAt') IS NULL
    ALTER TABLE [Reviews] ADD [UpdatedAt] datetime2 NULL;

IF COL_LENGTH('Reviews', 'UserId') IS NULL
    ALTER TABLE [Reviews] ADD [UserId] nvarchar(450) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reviews_UserId' AND object_id = OBJECT_ID(N'[Reviews]'))
    EXEC(N'CREATE INDEX [IX_Reviews_UserId] ON [Reviews] ([UserId])');

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reviews_ProductId_UserId' AND object_id = OBJECT_ID(N'[Reviews]'))
    EXEC(N'CREATE UNIQUE INDEX [IX_Reviews_ProductId_UserId] ON [Reviews] ([ProductId], [UserId]) WHERE [UserId] IS NOT NULL');

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Reviews_AspNetUsers_UserId' AND parent_object_id = OBJECT_ID(N'[Reviews]'))
    ALTER TABLE [Reviews] ADD CONSTRAINT [FK_Reviews_AspNetUsers_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Reviews_AspNetUsers_UserId' AND parent_object_id = OBJECT_ID(N'[Reviews]'))
    ALTER TABLE [Reviews] DROP CONSTRAINT [FK_Reviews_AspNetUsers_UserId];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reviews_ProductId_UserId' AND object_id = OBJECT_ID(N'[Reviews]'))
    DROP INDEX [IX_Reviews_ProductId_UserId] ON [Reviews];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reviews_UserId' AND object_id = OBJECT_ID(N'[Reviews]'))
    DROP INDEX [IX_Reviews_UserId] ON [Reviews];

IF COL_LENGTH('Reviews', 'UpdatedAt') IS NOT NULL
    ALTER TABLE [Reviews] DROP COLUMN [UpdatedAt];

IF COL_LENGTH('Reviews', 'UserId') IS NOT NULL
    ALTER TABLE [Reviews] DROP COLUMN [UserId];
");
        }
    }
}
