using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentUnionBot.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaToAppealMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentFileId",
                table: "AppealMessages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentFileName",
                table: "AppealMessages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoFileId",
                table: "AppealMessages",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentFileId",
                table: "AppealMessages");

            migrationBuilder.DropColumn(
                name: "DocumentFileName",
                table: "AppealMessages");

            migrationBuilder.DropColumn(
                name: "PhotoFileId",
                table: "AppealMessages");
        }
    }
}
