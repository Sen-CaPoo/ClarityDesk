using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClarityDesk.Migrations
{
    /// <inheritdoc />
    public partial class AddLineIntegrationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "IssueReports",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Web");

            migrationBuilder.CreateTable(
                name: "LineBindings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LineUserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PictureUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BindingStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BoundAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastInteractedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineBindings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LineConversationSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    LineUserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CurrentStep = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SessionData = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineConversationSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineConversationSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LineMessageLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    LineUserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IssueReportId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineMessageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineMessageLogs_IssueReports_IssueReportId",
                        column: x => x.IssueReportId,
                        principalTable: "IssueReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssueReports_Source",
                table: "IssueReports",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_LineBindings_BindingStatus",
                table: "LineBindings",
                column: "BindingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_LineBindings_LineUserId",
                table: "LineBindings",
                column: "LineUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LineBindings_UserId",
                table: "LineBindings",
                column: "UserId",
                unique: true,
                filter: "[BindingStatus] = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_LineConversationSessions_ExpiresAt",
                table: "LineConversationSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_LineConversationSessions_LineUserId_ExpiresAt",
                table: "LineConversationSessions",
                columns: new[] { "LineUserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LineConversationSessions_UserId",
                table: "LineConversationSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LineMessageLogs_IssueReportId",
                table: "LineMessageLogs",
                column: "IssueReportId",
                filter: "[IssueReportId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LineMessageLogs_LineUserId_SentAt",
                table: "LineMessageLogs",
                columns: new[] { "LineUserId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LineMessageLogs_SentAt_MessageType_IsSuccess",
                table: "LineMessageLogs",
                columns: new[] { "SentAt", "MessageType", "IsSuccess" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LineBindings");

            migrationBuilder.DropTable(
                name: "LineConversationSessions");

            migrationBuilder.DropTable(
                name: "LineMessageLogs");

            migrationBuilder.DropIndex(
                name: "IX_IssueReports_Source",
                table: "IssueReports");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "IssueReports");
        }
    }
}
