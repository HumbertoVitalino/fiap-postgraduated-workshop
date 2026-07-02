IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'IntegrationTestsDb')
    CREATE DATABASE IntegrationTestsDb;
GO

USE IntegrationTestsDb;
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'Users' AND xtype = 'U')
BEGIN
    CREATE TABLE Users (
        Id          UNIQUEIDENTIFIER NOT NULL,
        Email       NVARCHAR(256)    NOT NULL,
        Name        NVARCHAR(100)    NOT NULL,
        Role        NVARCHAR(20)     NOT NULL DEFAULT 'User',
        CreatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_Users PRIMARY KEY (Id)
    );

    CREATE UNIQUE INDEX IX_Users_Email ON Users (Email);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Users') AND name = 'Role'
)
BEGIN
    ALTER TABLE Users ADD Role NVARCHAR(20) NOT NULL DEFAULT 'User';
END
GO
