using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentUnionBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndexesForPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_News_IsArchived_IsPublished",
                table: "News",
                columns: new[] { "IsArchived", "IsPublished" });

            migrationBuilder.CreateIndex(
                name: "IX_News_IsPublished_Category_CreatedAt",
                table: "News",
                columns: new[] { "IsPublished", "Category", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_News_IsPublished_IsPinned_CreatedAt",
                table: "News",
                columns: new[] { "IsPublished", "IsPinned", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_IsFeatured_Status_StartDate",
                table: "Events",
                columns: new[] { "IsFeatured", "Status", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_IsPublished_StartDate",
                table: "Events",
                columns: new[] { "IsPublished", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_Status_StartDate",
                table: "Events",
                columns: new[] { "Status", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Appeals_StudentId_Status",
                table: "Appeals",
                columns: new[] { "StudentId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_News_IsArchived_IsPublished",
                table: "News");

            migrationBuilder.DropIndex(
                name: "IX_News_IsPublished_Category_CreatedAt",
                table: "News");

            migrationBuilder.DropIndex(
                name: "IX_News_IsPublished_IsPinned_CreatedAt",
                table: "News");

            migrationBuilder.DropIndex(
                name: "IX_Events_IsFeatured_Status_StartDate",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_IsPublished_StartDate",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_Status_StartDate",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Appeals_StudentId_Status",
                table: "Appeals");
        }
    }
}
