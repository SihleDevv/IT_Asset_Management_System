-- Comprehensive script to create ITSupportTickets table and add missing columns
-- This script handles the case where the table doesn't exist yet

USE [IT_Asset_Management_System_DB2];
GO

-- Check if table exists, if not create it
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ITSupportTickets')
BEGIN
    PRINT 'Creating ITSupportTickets table...';
    
    CREATE TABLE [dbo].[ITSupportTickets] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Subject] nvarchar(200) NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [Priority] nvarchar(50) NOT NULL,
        [AssetType] nvarchar(50) NOT NULL,
        [RelatedAssetId] int NOT NULL,
        [RelatedAssetName] nvarchar(200) NULL,
        [ReportedByUserId] nvarchar(450) NOT NULL,
        [AssignedToUserId] nvarchar(450) NULL,
        [AdminResponse] nvarchar(2000) NULL,
        [ResolutionNotes] nvarchar(2000) NULL,
        [TechnicianNotes] nvarchar(2000) NULL,
        [ReplacementRequested] bit NOT NULL DEFAULT 0,
        [ReplacementReason] nvarchar(2000) NULL,
        [ReplacementApproved] bit NULL,
        [ReplacementAdminResponse] nvarchar(2000) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        [ResolvedDate] datetime2 NULL,
        [UserFollowUp] nvarchar(2000) NULL,
        [FollowUpDate] datetime2 NULL,
        [LastActionDate] datetime2 NULL,
        [TechnicianNotesDate] datetime2 NULL,
        [StatusChangedDate] datetime2 NULL,
        [StatusChangedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_ITSupportTickets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ITSupportTickets_AspNetUsers_AssignedToUserId] 
            FOREIGN KEY ([AssignedToUserId]) 
            REFERENCES [dbo].[AspNetUsers] ([Id]),
        CONSTRAINT [FK_ITSupportTickets_AspNetUsers_ReportedByUserId] 
            FOREIGN KEY ([ReportedByUserId]) 
            REFERENCES [dbo].[AspNetUsers] ([Id]) 
            ON DELETE CASCADE,
        CONSTRAINT [FK_ITSupportTickets_AspNetUsers_StatusChangedByUserId] 
            FOREIGN KEY ([StatusChangedByUserId]) 
            REFERENCES [dbo].[AspNetUsers] ([Id])
    );
    
    -- Create indexes
    CREATE INDEX [IX_ITSupportTickets_AssignedToUserId] 
        ON [dbo].[ITSupportTickets] ([AssignedToUserId]);
    
    CREATE INDEX [IX_ITSupportTickets_ReportedByUserId] 
        ON [dbo].[ITSupportTickets] ([ReportedByUserId]);
    
    CREATE INDEX [IX_ITSupportTickets_StatusChangedByUserId] 
        ON [dbo].[ITSupportTickets] ([StatusChangedByUserId]);
    
    PRINT 'ITSupportTickets table created successfully.';
END
ELSE
BEGIN
    PRINT 'ITSupportTickets table already exists. Adding missing columns...';
    
    -- Add UserFollowUp column if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ITSupportTickets]') AND name = 'UserFollowUp')
    BEGIN
        ALTER TABLE [dbo].[ITSupportTickets] 
        ADD [UserFollowUp] nvarchar(2000) NULL;
        PRINT 'Added UserFollowUp column';
    END
    ELSE
    BEGIN
        PRINT 'UserFollowUp column already exists';
    END
    
    -- Add FollowUpDate column if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ITSupportTickets]') AND name = 'FollowUpDate')
    BEGIN
        ALTER TABLE [dbo].[ITSupportTickets] 
        ADD [FollowUpDate] datetime2 NULL;
        PRINT 'Added FollowUpDate column';
    END
    ELSE
    BEGIN
        PRINT 'FollowUpDate column already exists';
    END
    
    -- Add LastActionDate column if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ITSupportTickets]') AND name = 'LastActionDate')
    BEGIN
        ALTER TABLE [dbo].[ITSupportTickets] 
        ADD [LastActionDate] datetime2 NULL;
        PRINT 'Added LastActionDate column';
    END
    ELSE
    BEGIN
        PRINT 'LastActionDate column already exists';
    END
    
    -- Add TechnicianNotesDate column if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ITSupportTickets]') AND name = 'TechnicianNotesDate')
    BEGIN
        ALTER TABLE [dbo].[ITSupportTickets] 
        ADD [TechnicianNotesDate] datetime2 NULL;
        PRINT 'Added TechnicianNotesDate column';
    END
    ELSE
    BEGIN
        PRINT 'TechnicianNotesDate column already exists';
    END
    
    -- Add StatusChangedDate column if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ITSupportTickets]') AND name = 'StatusChangedDate')
    BEGIN
        ALTER TABLE [dbo].[ITSupportTickets] 
        ADD [StatusChangedDate] datetime2 NULL;
        PRINT 'Added StatusChangedDate column';
    END
    ELSE
    BEGIN
        PRINT 'StatusChangedDate column already exists';
    END
    
    -- Add StatusChangedByUserId column if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ITSupportTickets]') AND name = 'StatusChangedByUserId')
    BEGIN
        ALTER TABLE [dbo].[ITSupportTickets] 
        ADD [StatusChangedByUserId] nvarchar(450) NULL;
        PRINT 'Added StatusChangedByUserId column';
    END
    ELSE
    BEGIN
        PRINT 'StatusChangedByUserId column already exists';
    END
    
    -- Add foreign key for StatusChangedByUserId if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ITSupportTickets_AspNetUsers_StatusChangedByUserId')
    BEGIN
        ALTER TABLE [dbo].[ITSupportTickets]
        ADD CONSTRAINT [FK_ITSupportTickets_AspNetUsers_StatusChangedByUserId] 
            FOREIGN KEY ([StatusChangedByUserId]) 
            REFERENCES [dbo].[AspNetUsers] ([Id]);
        PRINT 'Added foreign key for StatusChangedByUserId';
    END
    ELSE
    BEGIN
        PRINT 'Foreign key for StatusChangedByUserId already exists';
    END
    
    -- Add index for StatusChangedByUserId if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ITSupportTickets_StatusChangedByUserId')
    BEGIN
        CREATE INDEX [IX_ITSupportTickets_StatusChangedByUserId] 
            ON [dbo].[ITSupportTickets] ([StatusChangedByUserId]);
        PRINT 'Added index for StatusChangedByUserId';
    END
    ELSE
    BEGIN
        PRINT 'Index for StatusChangedByUserId already exists';
    END
    
    -- Add TechnicianNotes if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ITSupportTickets]') AND name = 'TechnicianNotes')
    BEGIN
        ALTER TABLE [dbo].[ITSupportTickets] 
        ADD [TechnicianNotes] nvarchar(2000) NULL;
        PRINT 'Added TechnicianNotes column';
    END
    
    -- Add ReplacementRequested if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ITSupportTickets]') AND name = 'ReplacementRequested')
    BEGIN
        ALTER TABLE [dbo].[ITSupportTickets] 
        ADD [ReplacementRequested] bit NOT NULL DEFAULT 0;
        PRINT 'Added ReplacementRequested column';
    END
    
    -- Add ReplacementReason if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ITSupportTickets]') AND name = 'ReplacementReason')
    BEGIN
        ALTER TABLE [dbo].[ITSupportTickets] 
        ADD [ReplacementReason] nvarchar(2000) NULL;
        PRINT 'Added ReplacementReason column';
    END
    
    -- Add ReplacementApproved if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ITSupportTickets]') AND name = 'ReplacementApproved')
    BEGIN
        ALTER TABLE [dbo].[ITSupportTickets] 
        ADD [ReplacementApproved] bit NULL;
        PRINT 'Added ReplacementApproved column';
    END
    
    -- Add ReplacementAdminResponse if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ITSupportTickets]') AND name = 'ReplacementAdminResponse')
    BEGIN
        ALTER TABLE [dbo].[ITSupportTickets] 
        ADD [ReplacementAdminResponse] nvarchar(2000) NULL;
        PRINT 'Added ReplacementAdminResponse column';
    END
    
    -- Ensure AssetType and RelatedAssetId are NOT NULL (if they're nullable)
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ITSupportTickets]') AND name = 'AssetType' AND is_nullable = 1)
    BEGIN
        -- Update existing NULL values
        UPDATE [dbo].[ITSupportTickets] 
        SET [AssetType] = 'Computer' 
        WHERE [AssetType] IS NULL;
        
        ALTER TABLE [dbo].[ITSupportTickets] 
        ALTER COLUMN [AssetType] nvarchar(50) NOT NULL;
        PRINT 'Updated AssetType to NOT NULL';
    END
    
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ITSupportTickets]') AND name = 'RelatedAssetId' AND is_nullable = 1)
    BEGIN
        -- Update existing NULL values
        UPDATE [dbo].[ITSupportTickets] 
        SET [RelatedAssetId] = 0 
        WHERE [RelatedAssetId] IS NULL;
        
        ALTER TABLE [dbo].[ITSupportTickets] 
        ALTER COLUMN [RelatedAssetId] int NOT NULL;
        PRINT 'Updated RelatedAssetId to NOT NULL';
    END
END
GO

-- Set LastActionDate to CreatedDate for existing tickets where LastActionDate is NULL
UPDATE [dbo].[ITSupportTickets] 
SET [LastActionDate] = [CreatedDate] 
WHERE [LastActionDate] IS NULL;
GO

-- Update status from "Open" to "Pending" if any exist
UPDATE [dbo].[ITSupportTickets] 
SET [Status] = 'Pending' 
WHERE [Status] = 'Open';
GO

-- Add migration entries to __EFMigrationsHistory table if they don't exist
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260119104933_ITSupportTickets')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260119104933_ITSupportTickets', '8.0.0');
    PRINT 'Added migration history: 20260119104933_ITSupportTickets';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260119120000_AddITSupportTicket')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260119120000_AddITSupportTicket', '8.0.0');
    PRINT 'Added migration history: 20260119120000_AddITSupportTicket';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260119130000_UpdateITSupportTicketFields')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260119130000_UpdateITSupportTicketFields', '8.0.0');
    PRINT 'Added migration history: 20260119130000_UpdateITSupportTicketFields';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260119140000_MakeAssetFieldsRequiredInTickets')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260119140000_MakeAssetFieldsRequiredInTickets', '8.0.0');
    PRINT 'Added migration history: 20260119140000_MakeAssetFieldsRequiredInTickets';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260119150000_AddUserFollowUpToTickets')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260119150000_AddUserFollowUpToTickets', '8.0.0');
    PRINT 'Added migration history: 20260119150000_AddUserFollowUpToTickets';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260119160000_AddLastActionDateToTickets')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260119160000_AddLastActionDateToTickets', '8.0.0');
    PRINT 'Added migration history: 20260119160000_AddLastActionDateToTickets';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260119200000_AddTimestampFieldsToTickets')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260119200000_AddTimestampFieldsToTickets', '8.0.0');
    PRINT 'Added migration history: 20260119200000_AddTimestampFieldsToTickets';
END

PRINT 'Script completed successfully!';
GO
