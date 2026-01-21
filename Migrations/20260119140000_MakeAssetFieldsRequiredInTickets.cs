using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Asset_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class MakeAssetFieldsRequiredInTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if table exists and update columns to be non-nullable
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ITSupportTickets')
                BEGIN
                    -- Update existing NULL values to default values (if any exist)
                    UPDATE ITSupportTickets 
                    SET AssetType = 'Computer', RelatedAssetId = 0
                    WHERE AssetType IS NULL OR RelatedAssetId IS NULL;

                    -- Alter AssetType to NOT NULL
                    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ITSupportTickets]') AND name = 'AssetType' AND is_nullable = 1)
                    BEGIN
                        ALTER TABLE [ITSupportTickets] ALTER COLUMN [AssetType] nvarchar(50) NOT NULL;
                    END

                    -- Alter RelatedAssetId to NOT NULL
                    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ITSupportTickets]') AND name = 'RelatedAssetId' AND is_nullable = 1)
                    BEGIN
                        ALTER TABLE [ITSupportTickets] ALTER COLUMN [RelatedAssetId] int NOT NULL;
                    END
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to nullable
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ITSupportTickets')
                BEGIN
                    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ITSupportTickets]') AND name = 'AssetType' AND is_nullable = 0)
                    BEGIN
                        ALTER TABLE [ITSupportTickets] ALTER COLUMN [AssetType] nvarchar(50) NULL;
                    END

                    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ITSupportTickets]') AND name = 'RelatedAssetId' AND is_nullable = 0)
                    BEGIN
                        ALTER TABLE [ITSupportTickets] ALTER COLUMN [RelatedAssetId] int NULL;
                    END
                END
            ");
        }
    }
}
