using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SoilShop.Infrastructure.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20261023143700_CaseInsensitiveCodes")]
public sealed class CaseInsensitiveCodes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE UNIQUE INDEX ux_users_username_fold ON users (lower(username));
        CREATE UNIQUE INDEX ux_supplier_code_fold ON suppliers (upper(code));
        CREATE UNIQUE INDEX ux_product_code_fold ON products (upper(code));
        CREATE UNIQUE INDEX ux_sku_code_fold ON skus (upper(code));
        """);
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DROP INDEX ux_users_username_fold;
        DROP INDEX ux_supplier_code_fold;
        DROP INDEX ux_product_code_fold;
        DROP INDEX ux_sku_code_fold;
        """);
}
