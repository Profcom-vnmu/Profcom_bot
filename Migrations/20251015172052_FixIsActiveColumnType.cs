using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentUnionBot.Migrations
{
    /// <inheritdoc />
    public partial class FixIsActiveColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Конвертуємо integer колонку IsActive в boolean для PostgreSQL
            migrationBuilder.Sql("ALTER TABLE \"Users\" ALTER COLUMN \"IsActive\" TYPE boolean USING \"IsActive\"::boolean;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Повертаємо boolean колонку IsActive в integer
            migrationBuilder.Sql("ALTER TABLE \"Users\" ALTER COLUMN \"IsActive\" TYPE integer USING CASE WHEN \"IsActive\" THEN 1 ELSE 0 END;");
        }
    }
}
