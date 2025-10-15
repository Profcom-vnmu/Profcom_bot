using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentUnionBot.Migrations
{
    /// <inheritdoc />
    public partial class FixTelegramIdColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Конвертуємо integer колонки в bigint для PostgreSQL (long в C#)
            migrationBuilder.Sql("ALTER TABLE \"Users\" ALTER COLUMN \"TelegramId\" TYPE bigint;");
            migrationBuilder.Sql("ALTER TABLE \"Appeals\" ALTER COLUMN \"StudentId\" TYPE bigint;");
            migrationBuilder.Sql("ALTER TABLE \"Appeals\" ALTER COLUMN \"ClosedBy\" TYPE bigint;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Повертаємо bigint колонки в integer (може втратити дані!)
            migrationBuilder.Sql("ALTER TABLE \"Users\" ALTER COLUMN \"TelegramId\" TYPE integer;");
            migrationBuilder.Sql("ALTER TABLE \"Appeals\" ALTER COLUMN \"StudentId\" TYPE integer;");
            migrationBuilder.Sql("ALTER TABLE \"Appeals\" ALTER COLUMN \"ClosedBy\" TYPE integer;");
        }
    }
}
