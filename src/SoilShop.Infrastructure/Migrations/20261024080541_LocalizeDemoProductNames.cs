using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoilShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LocalizeDemoProductNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            Rename(migrationBuilder, reverse: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            Rename(migrationBuilder, reverse: true);
        }
        private static void Rename(MigrationBuilder migrationBuilder, bool reverse)
        {
            // Immutable fixture-name pairs. Match both code and old name to preserve staff edits.
            var names = new (string Code, string English, string Vietnamese)[]
            {
                ("SEED-V1-P01", "Everyday Potting Mix [demo]", "Đất trồng đa dụng [mẫu]"),
                ("SEED-V1-P02", "Leafy Plant Mix [demo]", "Đất trồng cây kiểng lá [mẫu]"),
                ("SEED-V1-P03", "Flowering Plant Mix [demo]", "Đất trồng cây hoa [mẫu]"),
                ("SEED-V1-P04", "Succulent & Cactus Mix [demo]", "Đất trồng sen đá và xương rồng [mẫu]"),
                ("SEED-V1-P05", "Orchid Potting Medium [demo]", "Giá thể trồng lan [mẫu]"),
                ("SEED-V1-P06", "Seed-starting Mix [demo]", "Giá thể ươm hạt [mẫu]"),
                ("SEED-V1-P07", "Lightweight Potting Mix [demo]", "Đất trồng đa dụng nhẹ [mẫu]"),
                ("SEED-V1-P08", "Indoor Foliage Mix [demo]", "Đất trồng kiểng lá trong nhà [mẫu]")
            };
            foreach (var item in names)
            {
                var before = reverse ? item.Vietnamese : item.English;
                var after = reverse ? item.English : item.Vietnamese;
                migrationBuilder.Sql($"UPDATE products SET name='{after}', version=version+1 WHERE code='{item.Code}' AND name='{before}';");
            }
            var previousPrefix = reverse ? "Đất trồng đa dụng (mẫu " : "All-purpose Potting Mix (demo ";
            var nextPrefix = reverse ? "All-purpose Potting Mix (demo " : "Đất trồng đa dụng (mẫu ";
            migrationBuilder.Sql($"UPDATE products SET name='{nextPrefix}' || substring(name FROM length('{previousPrefix}')+1), version=version+1 WHERE code LIKE 'DEMO-MIX-%' AND name LIKE '{previousPrefix}%';");
        }
    }
}
