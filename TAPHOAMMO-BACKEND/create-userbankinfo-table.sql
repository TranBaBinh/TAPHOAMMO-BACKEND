-- Script để tạo bảng UserBankInfos nếu chưa tồn tại
-- Chạy script này trong SQL Server Management Studio hoặc Azure Data Studio

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserBankInfos')
BEGIN
    CREATE TABLE [UserBankInfos] (
        [Id] int NOT NULL IDENTITY(1,1),
        [UserId] int NOT NULL,
        [BankName] nvarchar(100) NULL,
        [BankAccountNumber] nvarchar(50) NULL,
        [BankAccountHolder] nvarchar(100) NULL,
        [BankBranch] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UserBankInfos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserBankInfos_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
    
    CREATE UNIQUE INDEX [IX_UserBankInfos_UserId] ON [UserBankInfos] ([UserId]);
    
    PRINT 'Table UserBankInfos created successfully';
END
ELSE
BEGIN
    PRINT 'Table UserBankInfos already exists';
END

-- Di chuyển dữ liệu từ Users sang UserBankInfos (nếu có)
-- Sử dụng dynamic SQL để tránh lỗi khi các cột không tồn tại
DECLARE @sql NVARCHAR(MAX);
IF EXISTS (SELECT 1 FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'[Users]') 
           AND name = 'BankName')
BEGIN
    SET @sql = N'
        INSERT INTO [UserBankInfos] ([UserId], [BankName], [BankAccountNumber], [BankAccountHolder], [BankBranch], [CreatedAt], [UpdatedAt])
        SELECT [Id], [BankName], [BankAccountNumber], [BankAccountHolder], [BankBranch], GETUTCDATE(), GETUTCDATE()
        FROM [Users]
        WHERE ([BankName] IS NOT NULL 
           OR [BankAccountNumber] IS NOT NULL 
           OR [BankAccountHolder] IS NOT NULL 
           OR [BankBranch] IS NOT NULL)
          AND [Id] NOT IN (SELECT [UserId] FROM [UserBankInfos]);
    ';
    EXEC sp_executesql @sql;
    PRINT 'Data migrated from Users to UserBankInfos';
END
ELSE
BEGIN
    PRINT 'Bank columns do not exist in Users table. Skipping data migration.';
END

