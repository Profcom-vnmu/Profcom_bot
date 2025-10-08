using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentUnionBot.Migrations
{
    /// <inheritdoc />
    public partial class AddAppealMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "Appeals",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppealMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AppealId = table.Column<int>(type: "INTEGER", nullable: false),
                    SenderId = table.Column<long>(type: "INTEGER", nullable: false),
                    SenderName = table.Column<string>(type: "TEXT", nullable: false),
                    IsFromAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppealMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppealMessages_Appeals_AppealId",
                        column: x => x.AppealId,
                        principalTable: "Appeals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppealMessages_AppealId",
                table: "AppealMessages",
                column: "AppealId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppealMessages");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "Appeals");
        }
    }
}
