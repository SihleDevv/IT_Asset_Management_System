-- SQL Script to add Password Expiration columns to AspNetUsers table
-- Run this script on your database to add the new columns
-- Make sure you're connected to the correct database before running

USE [IT_Asset_Management_System_DB2];
GO

-- Step 1: Add PasswordChangedDate column (nullable DateTime)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'PasswordChangedDate')
BEGIN
    ALTER TABLE [AspNetUsers]
    ADD [PasswordChangedDate] datetime2 NULL;
    PRINT 'PasswordChangedDate column added successfully.';
END
ELSE
BEGIN
    PRINT 'PasswordChangedDate column already exists.';
END
GO

-- Step 2: Update existing users to have PasswordChangedDate set to current date
-- Using dynamic SQL to avoid validation errors
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'PasswordChangedDate')
BEGIN
    DECLARE @UpdateSQL NVARCHAR(MAX);
    
    -- Check if CreatedDate column exists
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'CreatedDate')
    BEGIN
        SET @UpdateSQL = N'UPDATE [AspNetUsers] SET [PasswordChangedDate] = [CreatedDate] WHERE [PasswordChangedDate] IS NULL;';
    END
    ELSE
    BEGIN
        SET @UpdateSQL = N'UPDATE [AspNetUsers] SET [PasswordChangedDate] = GETDATE() WHERE [PasswordChangedDate] IS NULL;';
    END
    
    EXEC sp_executesql @UpdateSQL;
    PRINT 'Updated existing users with PasswordChangedDate.';
END
GO

-- Step 3: Add MustChangePassword column (bit, default false)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'MustChangePassword')
BEGIN
    ALTER TABLE [AspNetUsers]
    ADD [MustChangePassword] bit NOT NULL DEFAULT 0;
    PRINT 'MustChangePassword column added successfully.';
END
ELSE
BEGIN
    PRINT 'MustChangePassword column already exists.';
END
GO

PRINT 'Migration completed successfully.';
