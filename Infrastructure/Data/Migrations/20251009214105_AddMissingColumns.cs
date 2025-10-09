using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentUnionBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "News",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "NewsId",
                table: "FileAttachments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Events",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Appeals",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_NewsId",
                table: "FileAttachments",
                column: "NewsId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Category",
                table: "Events",
                column: "Category");

            migrationBuilder.AddForeignKey(
                name: "FK_FileAttachments_News_NewsId",
                table: "FileAttachments",
                column: "NewsId",
                principalTable: "News",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileAttachments_News_NewsId",
                table: "FileAttachments");

            migrationBuilder.DropIndex(
                name: "IX_FileAttachments_NewsId",
                table: "FileAttachments");

            migrationBuilder.DropIndex(
                name: "IX_Events_Category",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "News");

            migrationBuilder.DropColumn(
                name: "NewsId",
                table: "FileAttachments");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Appeals");
        }
    }
}
