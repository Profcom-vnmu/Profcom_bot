using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentUnionBot.Migrations
{
    /// <inheritdoc />
    public partial class RemoveObsoleteFieldsFromAppeal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RespondedAt",
                table: "Appeals");

            migrationBuilder.DropColumn(
                name: "Response",
                table: "Appeals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RespondedAt",
                table: "Appeals",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Response",
                table: "Appeals",
                type: "TEXT",
                nullable: true);
        }
    }
}
