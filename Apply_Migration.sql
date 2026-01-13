-- SQL Script to manually apply the migration changes
-- Run this script on your database if you cannot use EF Core migrations

-- Step 1: Add VendorType column to Assets table
ALTER TABLE [Assets]
ADD [VendorType] nvarchar(100) NOT NULL DEFAULT '';

-- Step 2: Make PurchasePrice nullable
-- First, update any NULL values to 0 (if needed)
-- UPDATE [Assets] SET [PurchasePrice] = 0 WHERE [PurchasePrice] IS NULL;

-- Then alter the column to allow NULL
ALTER TABLE [Assets]
ALTER COLUMN [PurchasePrice] decimal(18,2) NULL;

-- Verify the changes
-- SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
-- FROM INFORMATION_SCHEMA.COLUMNS 
-- WHERE TABLE_NAME = 'Assets' 
-- AND COLUMN_NAME IN ('VendorType', 'PurchasePrice');
