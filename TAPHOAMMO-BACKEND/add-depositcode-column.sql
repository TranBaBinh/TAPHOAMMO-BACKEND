-- Script to add DepositCode column to Wallet table
-- Run this script in your SQL Server database

USE [YourDatabaseName]; -- Thay đổi tên database của bạn
GO

-- Check if column exists, if not add it
IF NOT EXISTS (
    SELECT * 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Wallets' 
    AND COLUMN_NAME = 'DepositCode'
)
BEGIN
    ALTER TABLE [Wallets]
    ADD [DepositCode] NVARCHAR(50) NULL;
    
    -- Create index for faster lookup
    CREATE INDEX [IX_Wallets_DepositCode] ON [Wallets] ([DepositCode]);
    
    PRINT 'Column DepositCode added successfully!';
END
ELSE
BEGIN
    PRINT 'Column DepositCode already exists!';
END
GO

