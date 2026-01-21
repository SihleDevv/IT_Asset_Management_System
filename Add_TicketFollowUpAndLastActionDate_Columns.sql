-- Add UserFollowUp and FollowUpDate columns to ITSupportTickets table
-- Migration: 20260119150000_AddUserFollowUpToTickets

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ITSupportTickets]') AND name = 'UserFollowUp')
BEGIN
    ALTER TABLE [ITSupportTickets] 
    ADD [UserFollowUp] nvarchar(2000) NULL;
    PRINT 'Added UserFollowUp column';
END
ELSE
BEGIN
    PRINT 'UserFollowUp column already exists';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ITSupportTickets]') AND name = 'FollowUpDate')
BEGIN
    ALTER TABLE [ITSupportTickets] 
    ADD [FollowUpDate] datetime2 NULL;
    PRINT 'Added FollowUpDate column';
END
ELSE
BEGIN
    PRINT 'FollowUpDate column already exists';
END
GO

-- Add LastActionDate column to ITSupportTickets table
-- Migration: 20260119160000_AddLastActionDateToTickets

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ITSupportTickets]') AND name = 'LastActionDate')
BEGIN
    ALTER TABLE [ITSupportTickets] 
    ADD [LastActionDate] datetime2 NULL;
    PRINT 'Added LastActionDate column';
END
ELSE
BEGIN
    PRINT 'LastActionDate column already exists';
END
GO

-- Set LastActionDate to CreatedDate for existing tickets
UPDATE [ITSupportTickets] 
SET [LastActionDate] = [CreatedDate] 
WHERE [LastActionDate] IS NULL;
GO

PRINT 'Migration completed successfully!';
