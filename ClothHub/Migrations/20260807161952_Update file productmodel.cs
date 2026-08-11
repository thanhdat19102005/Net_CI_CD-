using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothHub.Migrations
{
    /// <inheritdoc />
    public partial class Updatefileproductmodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Suppliers_SupplierId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_SupplierId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "Products");

            migrationBuilder.AddColumn<string>(
                name: "SupplierModelId",
                table: "Products",
                type: "nvarchar(20)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_SupplierModelId",
                table: "Products",
                column: "SupplierModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Suppliers_SupplierModelId",
                table: "Products",
                column: "SupplierModelId",
                principalTable: "Suppliers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Suppliers_SupplierModelId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_SupplierModelId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SupplierModelId",
                table: "Products");

            migrationBuilder.AddColumn<string>(
                name: "SupplierId",
                table: "Products",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_SupplierId",
                table: "Products",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Suppliers_SupplierId",
                table: "Products",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
