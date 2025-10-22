using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClarityDesk.Migrations
{
    /// <inheritdoc />
    public partial class AddLastModifiedByUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastModifiedByUserId",
                table: "IssueReports",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssueReports_LastModifiedByUserId",
                table: "IssueReports",
                column: "LastModifiedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_IssueReports_Users_LastModifiedByUserId",
                table: "IssueReports",
                column: "LastModifiedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IssueReports_Users_LastModifiedByUserId",
                table: "IssueReports");

            migrationBuilder.DropIndex(
                name: "IX_IssueReports_LastModifiedByUserId",
                table: "IssueReports");

            migrationBuilder.DropColumn(
                name: "LastModifiedByUserId",
                table: "IssueReports");
        }
    }
}
