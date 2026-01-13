-- Migration script to remove VendorType column from BaseAssets table
-- This script removes the VendorType column that was added but is no longer needed
-- The Vendor column will now be dynamic based on Asset Type

USE [IT_Asset_Management_System]
GO

-- Check if the column exists before attempting to drop it
IF EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'BaseAssets' 
    AND COLUMN_NAME = 'VendorType'
)
BEGIN
    ALTER TABLE [dbo].[BaseAssets] DROP COLUMN [VendorType];
    PRINT 'VendorType column has been removed from BaseAssets table.';
END
ELSE
BEGIN
    PRINT 'VendorType column does not exist in BaseAssets table.';
END
GO
