using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Asset_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordExpirationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add PasswordChangedDate column to AspNetUsers table
            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordChangedDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            // Add MustChangePassword column to AspNetUsers table
            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Update existing users to have PasswordChangedDate set to current date
            migrationBuilder.Sql(@"
                UPDATE [AspNetUsers]
                SET [PasswordChangedDate] = GETDATE()
                WHERE [PasswordChangedDate] IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove MustChangePassword column
            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "AspNetUsers");

            // Remove PasswordChangedDate column
            migrationBuilder.DropColumn(
                name: "PasswordChangedDate",
                table: "AspNetUsers");
        }
    }
}
