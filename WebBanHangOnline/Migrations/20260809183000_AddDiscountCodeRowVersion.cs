using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanHangOnline.Migrations
{
    public partial class AddDiscountCodeRowVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('DiscountCodes', 'RowVersion') IS NULL
    ALTER TABLE [DiscountCodes] ADD [RowVersion] rowversion NOT NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('DiscountCodes', 'RowVersion') IS NOT NULL
    ALTER TABLE [DiscountCodes] DROP COLUMN [RowVersion];
");
        }
    }
}
