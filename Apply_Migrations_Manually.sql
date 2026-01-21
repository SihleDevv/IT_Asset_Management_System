-- First, add the columns
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ITSupportTickets]') AND name = 'UserFollowUp')
BEGIN
    ALTER TABLE [ITSupportTickets] ADD [UserFollowUp] nvarchar(2000) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ITSupportTickets]') AND name = 'FollowUpDate')
BEGIN
    ALTER TABLE [ITSupportTickets] ADD [FollowUpDate] datetime2 NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ITSupportTickets]') AND name = 'LastActionDate')
BEGIN
    ALTER TABLE [ITSupportTickets] ADD [LastActionDate] datetime2 NULL;
END
GO

-- Set LastActionDate to CreatedDate for existing tickets
UPDATE [ITSupportTickets] 
SET [LastActionDate] = [CreatedDate] 
WHERE [LastActionDate] IS NULL;
GO

-- Add migration entries to __EFMigrationsHistory table
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260119150000_AddUserFollowUpToTickets')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260119150000_AddUserFollowUpToTickets', '8.0.0');
END
GO

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260119160000_AddLastActionDateToTickets')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260119160000_AddLastActionDateToTickets', '8.0.0');
END
GO

PRINT 'Migrations applied successfully!';
