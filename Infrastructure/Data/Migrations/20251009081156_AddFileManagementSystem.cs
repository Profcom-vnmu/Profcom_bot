using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentUnionBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFileManagementSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    FileType = table.Column<int>(type: "INTEGER", nullable: false),
                    FileHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IsCompressed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ThumbnailPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UploadedByUserId = table.Column<long>(type: "INTEGER", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ScanStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    ScanResult = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileAttachments_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "TelegramId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppealFileAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AppealId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileAttachmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    AttachedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AttachedByUserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsEvidence = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppealFileAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppealFileAttachments_Appeals_AppealId",
                        column: x => x.AppealId,
                        principalTable: "Appeals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppealFileAttachments_FileAttachments_FileAttachmentId",
                        column: x => x.FileAttachmentId,
                        principalTable: "FileAttachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppealFileAttachments_Users_AttachedByUserId",
                        column: x => x.AttachedByUserId,
                        principalTable: "Users",
                        principalColumn: "TelegramId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppealFileAttachments_AppealId",
                table: "AppealFileAttachments",
                column: "AppealId");

            migrationBuilder.CreateIndex(
                name: "IX_AppealFileAttachments_AppealId_FileAttachmentId",
                table: "AppealFileAttachments",
                columns: new[] { "AppealId", "FileAttachmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppealFileAttachments_AttachedAt",
                table: "AppealFileAttachments",
                column: "AttachedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AppealFileAttachments_AttachedByUserId",
                table: "AppealFileAttachments",
                column: "AttachedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppealFileAttachments_FileAttachmentId",
                table: "AppealFileAttachments",
                column: "FileAttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppealFileAttachments_IsEvidence",
                table: "AppealFileAttachments",
                column: "IsEvidence");

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_FileHash",
                table: "FileAttachments",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_FileType",
                table: "FileAttachments",
                column: "FileType");

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_IsDeleted",
                table: "FileAttachments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_IsDeleted_FileType",
                table: "FileAttachments",
                columns: new[] { "IsDeleted", "FileType" });

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_IsDeleted_ScanStatus",
                table: "FileAttachments",
                columns: new[] { "IsDeleted", "ScanStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_ScanStatus",
                table: "FileAttachments",
                column: "ScanStatus");

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_UploadedAt",
                table: "FileAttachments",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_UploadedByUserId",
                table: "FileAttachments",
                column: "UploadedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppealFileAttachments");

            migrationBuilder.DropTable(
                name: "FileAttachments");
        }
    }
}
