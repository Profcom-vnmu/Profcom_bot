using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentUnionBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Event = table.Column<int>(type: "INTEGER", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    PushEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SmsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    InAppEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "TelegramId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Event = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Data = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    RelatedAppealId = table.Column<int>(type: "INTEGER", nullable: true),
                    RelatedNewsId = table.Column<int>(type: "INTEGER", nullable: true),
                    RelatedEventId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Appeals_RelatedAppealId",
                        column: x => x.RelatedAppealId,
                        principalTable: "Appeals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_Events_RelatedEventId",
                        column: x => x.RelatedEventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_News_RelatedNewsId",
                        column: x => x.RelatedNewsId,
                        principalTable: "News",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "TelegramId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Event = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    TitleTemplate = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    MessageTemplate = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_Event",
                table: "NotificationPreferences",
                column: "Event");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId",
                table: "NotificationPreferences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId_Event",
                table: "NotificationPreferences",
                columns: new[] { "UserId", "Event" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Event",
                table: "Notifications",
                column: "Event");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RelatedAppealId",
                table: "Notifications",
                column: "RelatedAppealId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RelatedEventId",
                table: "Notifications",
                column: "RelatedEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RelatedNewsId",
                table: "Notifications",
                column: "RelatedNewsId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ScheduledFor",
                table: "Notifications",
                column: "ScheduledFor");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Status",
                table: "Notifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Status_ScheduledFor",
                table: "Notifications",
                columns: new[] { "Status", "ScheduledFor" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type",
                table: "Notifications",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_Status",
                table: "Notifications",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Event",
                table: "NotificationTemplates",
                column: "Event");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Event_Type_Language",
                table: "NotificationTemplates",
                columns: new[] { "Event", "Type", "Language" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_IsActive",
                table: "NotificationTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Language",
                table: "NotificationTemplates",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Type",
                table: "NotificationTemplates",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "NotificationTemplates");
        }
    }
}
