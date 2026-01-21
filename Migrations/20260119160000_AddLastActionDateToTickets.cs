using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Asset_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddLastActionDateToTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastActionDate",
                table: "ITSupportTickets",
                type: "datetime2",
                nullable: true);

            // Set LastActionDate to CreatedDate for existing tickets
            migrationBuilder.Sql(@"
                UPDATE ITSupportTickets 
                SET LastActionDate = CreatedDate 
                WHERE LastActionDate IS NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastActionDate",
                table: "ITSupportTickets");
        }
    }
}
