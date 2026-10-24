using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoilShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "product_type",
                table: "products",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "'Unclassified'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_5",
                table: "products",
                sql: "product_type IN ('Unclassified','GeneralPurpose','FoliageMix','FloweringMix','SucculentMix','OrchidMix','SeedStartingMix')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_products_5",
                table: "products");

            migrationBuilder.DropColumn(
                name: "product_type",
                table: "products");
        }
    }
}
