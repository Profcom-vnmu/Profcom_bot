using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentUnionBot.Migrations
{
    /// <inheritdoc />
    public partial class AddClosedByAndReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ClosedBy",
                table: "Appeals",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosedReason",
                table: "Appeals",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosedBy",
                table: "Appeals");

            migrationBuilder.DropColumn(
                name: "ClosedReason",
                table: "Appeals");
        }
    }
}
