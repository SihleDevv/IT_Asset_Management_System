using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Asset_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddTimestampFieldsToTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TechnicianNotesDate",
                table: "ITSupportTickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StatusChangedDate",
                table: "ITSupportTickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusChangedByUserId",
                table: "ITSupportTickets",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ITSupportTickets_StatusChangedByUserId",
                table: "ITSupportTickets",
                column: "StatusChangedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ITSupportTickets_AspNetUsers_StatusChangedByUserId",
                table: "ITSupportTickets",
                column: "StatusChangedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ITSupportTickets_AspNetUsers_StatusChangedByUserId",
                table: "ITSupportTickets");

            migrationBuilder.DropIndex(
                name: "IX_ITSupportTickets_StatusChangedByUserId",
                table: "ITSupportTickets");

            migrationBuilder.DropColumn(
                name: "TechnicianNotesDate",
                table: "ITSupportTickets");

            migrationBuilder.DropColumn(
                name: "StatusChangedDate",
                table: "ITSupportTickets");

            migrationBuilder.DropColumn(
                name: "StatusChangedByUserId",
                table: "ITSupportTickets");
        }
    }
}
