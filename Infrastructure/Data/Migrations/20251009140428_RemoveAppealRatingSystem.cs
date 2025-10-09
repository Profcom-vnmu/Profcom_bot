using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentUnionBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAppealRatingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Appeals");

            migrationBuilder.DropColumn(
                name: "RatingComment",
                table: "Appeals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "Appeals",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RatingComment",
                table: "Appeals",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);
        }
    }
}
