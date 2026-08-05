using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanHangOnline.Migrations
{
    [Migration("20260805093000_AddOrderPaymentStatus")]
    public partial class AddOrderPaymentStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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
        ELSE N''Unpaid''
    END
WHERE [PaymentStatus] IS NULL OR [PaymentStatus] = N''Unpaid'';
');

UPDATE [Orders]
SET [Status] =
    CASE
        WHEN [Status] IN (N'Paid', N'Failed', N'Refunded') THEN N'Confirmed'
        ELSE [Status]
    END;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Orders', 'PaymentStatus') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Orders_PaymentStatus')
        ALTER TABLE [Orders] DROP CONSTRAINT [DF_Orders_PaymentStatus];

    ALTER TABLE [Orders] DROP COLUMN [PaymentStatus];
END
");
        }
    }
}
