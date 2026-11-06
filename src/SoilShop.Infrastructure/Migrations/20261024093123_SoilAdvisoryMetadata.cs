using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoilShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SoilAdvisoryMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "aeration",
                table: "products",
                type: "varchar(12)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "biology_notes",
                table: "products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cleanliness_notes",
                table: "products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ec_max",
                table: "products",
                type: "numeric(8,3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ec_method",
                table: "products",
                type: "varchar(16)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ec_min",
                table: "products",
                type: "numeric(8,3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nutrition_notes",
                table: "products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "organic_matter_notes",
                table: "products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "sample_available",
                table: "products",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "structure_notes",
                table: "products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "use_context",
                table: "products",
                type: "varchar(12)",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_6",
                table: "products",
                sql: "aeration IS NULL OR aeration IN ('Low','Medium','High')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_7",
                table: "products",
                sql: "use_context IS NULL OR use_context IN ('Pot','InGround','Both')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_8",
                table: "products",
                sql: "(ec_min IS NULL AND ec_max IS NULL AND ec_method IS NULL) OR (ec_min IS NOT NULL AND ec_max IS NOT NULL AND ec_min >= 0 AND ec_max >= ec_min AND ec_method IS NOT NULL AND ec_method IN ('SME','Extract1to2','PourThrough','Other'))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_products_6",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_7",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_8",
                table: "products");

            migrationBuilder.DropColumn(
                name: "aeration",
                table: "products");

            migrationBuilder.DropColumn(
                name: "biology_notes",
                table: "products");

            migrationBuilder.DropColumn(
                name: "cleanliness_notes",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ec_max",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ec_method",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ec_min",
                table: "products");

            migrationBuilder.DropColumn(
                name: "nutrition_notes",
                table: "products");

            migrationBuilder.DropColumn(
                name: "organic_matter_notes",
                table: "products");

            migrationBuilder.DropColumn(
                name: "sample_available",
                table: "products");

            migrationBuilder.DropColumn(
                name: "structure_notes",
                table: "products");

            migrationBuilder.DropColumn(
                name: "use_context",
                table: "products");
        }
    }
}
