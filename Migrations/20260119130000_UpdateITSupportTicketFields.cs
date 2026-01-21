using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Asset_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class UpdateITSupportTicketFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TechnicianNotes",
                table: "ITSupportTickets",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReplacementRequested",
                table: "ITSupportTickets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReplacementReason",
                table: "ITSupportTickets",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReplacementApproved",
                table: "ITSupportTickets",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReplacementAdminResponse",
                table: "ITSupportTickets",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            // Update default status from "Open" to "Pending" for existing tickets
            migrationBuilder.Sql("UPDATE ITSupportTickets SET Status = 'Pending' WHERE Status = 'Open'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TechnicianNotes",
                table: "ITSupportTickets");

            migrationBuilder.DropColumn(
                name: "ReplacementRequested",
                table: "ITSupportTickets");

            migrationBuilder.DropColumn(
                name: "ReplacementReason",
                table: "ITSupportTickets");

            migrationBuilder.DropColumn(
                name: "ReplacementApproved",
                table: "ITSupportTickets");

            migrationBuilder.DropColumn(
                name: "ReplacementAdminResponse",
                table: "ITSupportTickets");
        }
    }
}
