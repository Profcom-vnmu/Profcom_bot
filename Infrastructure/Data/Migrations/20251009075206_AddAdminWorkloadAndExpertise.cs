using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentUnionBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminWorkloadAndExpertise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Language",
                table: "Users",
                type: "INTEGER",
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 5);

            migrationBuilder.CreateTable(
                name: "AdminWorkloads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AdminId = table.Column<long>(type: "INTEGER", nullable: false),
                    ActiveAppealsCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    TotalAppealsCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    IsAvailable = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    LastAssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminWorkloads", x => x.Id);
                    table.UniqueConstraint("AK_AdminWorkloads_AdminId", x => x.AdminId);
                    table.ForeignKey(
                        name: "FK_AdminWorkloads_Users_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Users",
                        principalColumn: "TelegramId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdminCategoryExpertises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AdminId = table.Column<long>(type: "INTEGER", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    ExperienceLevel = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    SuccessfulResolutions = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    TotalResolutions = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminCategoryExpertises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminCategoryExpertises_AdminWorkloads_AdminId",
                        column: x => x.AdminId,
                        principalTable: "AdminWorkloads",
                        principalColumn: "AdminId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdminCategoryExpertises_Users_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Users",
                        principalColumn: "TelegramId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminCategoryExpertises_AdminId_Category",
                table: "AdminCategoryExpertises",
                columns: new[] { "AdminId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminCategoryExpertises_Category",
                table: "AdminCategoryExpertises",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_AdminCategoryExpertises_ExperienceLevel",
                table: "AdminCategoryExpertises",
                column: "ExperienceLevel");

            migrationBuilder.CreateIndex(
                name: "IX_AdminWorkloads_AdminId",
                table: "AdminWorkloads",
                column: "AdminId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminWorkloads_IsAvailable",
                table: "AdminWorkloads",
                column: "IsAvailable");

            migrationBuilder.CreateIndex(
                name: "IX_AdminWorkloads_LastActivityAt",
                table: "AdminWorkloads",
                column: "LastActivityAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminCategoryExpertises");

            migrationBuilder.DropTable(
                name: "AdminWorkloads");

            migrationBuilder.AlterColumn<string>(
                name: "Language",
                table: "Users",
                type: "TEXT",
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldMaxLength: 5);
        }
    }
}
