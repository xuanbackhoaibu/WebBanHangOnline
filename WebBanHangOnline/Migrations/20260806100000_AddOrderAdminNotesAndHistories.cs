using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanHangOnline.Migrations
{
    [Migration("20260806100000_AddOrderAdminNotesAndHistories")]
    public partial class AddOrderAdminNotesAndHistories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Orders', 'AdminNote') IS NULL
BEGIN
    ALTER TABLE [Orders] ADD [AdminNote] nvarchar(500) NULL;
END
");

            migrationBuilder.Sql(@"
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
BEGIN
    CREATE INDEX [IX_OrderStatusHistories_OrderId] ON [OrderStatusHistories] ([OrderId]);
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[OrderStatusHistories]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [OrderStatusHistories];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Orders', 'AdminNote') IS NOT NULL
BEGIN
    ALTER TABLE [Orders] DROP COLUMN [AdminNote];
END
");
        }
    }
}
