using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanHangOnline.Migrations
{
    [Migration("20260724114500_AddProductFlashSaleFields")]
    public partial class AddProductFlashSaleFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FlashSaleEnd",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FlashSalePrice",
                table: "Products",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FlashSaleStart",
                table: "Products",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FlashSaleEnd",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "FlashSalePrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "FlashSaleStart",
                table: "Products");
        }
    }
}
