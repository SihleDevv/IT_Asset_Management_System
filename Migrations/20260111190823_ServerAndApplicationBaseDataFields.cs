using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Asset_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class ServerAndApplicationBaseDataFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackupComments",
                table: "Servers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "BackupRequired",
                table: "Servers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProjectManagerName",
                table: "Servers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationOwner",
                table: "Applications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BusinessUnit",
                table: "Applications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LicenseHolder",
                table: "Applications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackupComments",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "BackupRequired",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "ProjectManagerName",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "ApplicationOwner",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "BusinessUnit",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "LicenseHolder",
                table: "Applications");
        }
    }
}
