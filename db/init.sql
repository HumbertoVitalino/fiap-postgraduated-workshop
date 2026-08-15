-- Fonte única de verdade do schema, usada tanto pelo ambiente docker-compose (via sqlserver-init)
-- quanto pelos testes de integração (via DatabaseFixture). Mantenha em sincronia com
-- AppDbContext.OnModelCreating sempre que o modelo mudar.

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'$(DatabaseName)')
BEGIN
    EXEC('CREATE DATABASE [$(DatabaseName)]');
END
GO

USE $(DatabaseName);
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'Users' AND xtype = 'U')
BEGIN
    CREATE TABLE Users (
        Id          UNIQUEIDENTIFIER NOT NULL,
        Email       NVARCHAR(256)    NOT NULL,
        Name        NVARCHAR(100)    NOT NULL,
        Password    NVARCHAR(60)     NOT NULL,
        Role        NVARCHAR(20)     NOT NULL,
        CreatedAt   DATETIME2        NOT NULL,
        UpdatedAt   DATETIME2        NOT NULL,
        CONSTRAINT PK_Users PRIMARY KEY (Id)
    );

    CREATE UNIQUE INDEX IX_Users_Email ON Users (Email);
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'Customers' AND xtype = 'U')
BEGIN
    CREATE TABLE Customers (
        Id          UNIQUEIDENTIFIER NOT NULL,
        Name        NVARCHAR(150)    NOT NULL,
        Document    NVARCHAR(14)     NOT NULL,
        Email       NVARCHAR(256)    NOT NULL,
        Phone       NVARCHAR(20)     NOT NULL,
        CreatedAt   DATETIME2        NOT NULL,
        UpdatedAt   DATETIME2        NOT NULL,
        CONSTRAINT PK_Customers PRIMARY KEY (Id)
    );

    CREATE UNIQUE INDEX IX_Customers_Document ON Customers (Document);
    CREATE UNIQUE INDEX IX_Customers_Email ON Customers (Email);
    CREATE UNIQUE INDEX IX_Customers_Phone ON Customers (Phone);
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'Vehicles' AND xtype = 'U')
BEGIN
    CREATE TABLE Vehicles (
        Id               UNIQUEIDENTIFIER NOT NULL,
        CustomerId       UNIQUEIDENTIFIER NOT NULL,
        LicensePlate     NVARCHAR(10)     NOT NULL,
        Brand            NVARCHAR(100)    NOT NULL,
        Model            NVARCHAR(100)    NOT NULL,
        ManufactureYear  INT              NOT NULL,
        ModelYear        INT              NOT NULL,
        Color            NVARCHAR(50)     NOT NULL,
        CreatedAt        DATETIME2        NOT NULL,
        UpdatedAt        DATETIME2        NOT NULL,
        CONSTRAINT PK_Vehicles PRIMARY KEY (Id),
        CONSTRAINT FK_Vehicles_Customers_CustomerId FOREIGN KEY (CustomerId)
            REFERENCES Customers (Id) ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX IX_Vehicles_LicensePlate ON Vehicles (LicensePlate);
    CREATE INDEX IX_Vehicles_CustomerId ON Vehicles (CustomerId);
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'InventoryItems' AND xtype = 'U')
BEGIN
    CREATE TABLE InventoryItems (
        Id               UNIQUEIDENTIFIER NOT NULL,
        Code             NVARCHAR(50)     NOT NULL,
        Name             NVARCHAR(200)    NOT NULL,
        Description      NVARCHAR(1000)   NOT NULL,
        QuantityOnHand   INT              NOT NULL,
        ReservedQuantity INT              NOT NULL,
        MinimumStock     INT              NOT NULL,
        UnitPrice        DECIMAL(18,2)    NOT NULL,
        UnitOfMeasure    NVARCHAR(20)     NOT NULL,
        IsActive         BIT              NOT NULL,
        CreatedAt        DATETIME2        NOT NULL,
        UpdatedAt        DATETIME2        NOT NULL,
        CONSTRAINT PK_InventoryItems PRIMARY KEY (Id)
    );

    CREATE UNIQUE INDEX IX_InventoryItems_Code ON InventoryItems (Code);
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'Services' AND xtype = 'U')
BEGIN
    CREATE TABLE Services (
        Id                UNIQUEIDENTIFIER NOT NULL,
        Code              NVARCHAR(50)     NOT NULL,
        Name              NVARCHAR(200)    NOT NULL,
        Description       NVARCHAR(1000)   NOT NULL,
        BasePrice         DECIMAL(18,2)    NOT NULL,
        EstimatedDuration SMALLINT         NOT NULL,
        IsActive          BIT              NOT NULL,
        CreatedAt         DATETIME2        NOT NULL,
        UpdatedAt         DATETIME2        NOT NULL,
        CONSTRAINT PK_Services PRIMARY KEY (Id)
    );

    CREATE UNIQUE INDEX IX_Services_Code ON Services (Code);
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'ServiceOrders' AND xtype = 'U')
BEGIN
    CREATE TABLE ServiceOrders (
        Id                  UNIQUEIDENTIFIER NOT NULL,
        CustomerId          UNIQUEIDENTIFIER NOT NULL,
        VehicleId           UNIQUEIDENTIFIER NOT NULL,
        CreatedBy           UNIQUEIDENTIFIER NOT NULL,
        Status              NVARCHAR(20)     NOT NULL,
        ProblemDescription  NVARCHAR(2000)   NOT NULL,
        DiagnoseDescription NVARCHAR(2000)   NULL,
        OdometerReading     INT              NOT NULL,
        Discount            DECIMAL(18,2)    NOT NULL,
        Subtotal            DECIMAL(18,2)    NOT NULL,
        Total               DECIMAL(18,2)    NOT NULL,
        OpenedAt            DATETIME2        NOT NULL,
        ClosedAt            DATETIME2        NULL,
        CreatedAt           DATETIME2        NOT NULL,
        UpdatedAt           DATETIME2        NOT NULL,
        CONSTRAINT PK_ServiceOrders PRIMARY KEY (Id),
        CONSTRAINT FK_ServiceOrders_Customers_CustomerId FOREIGN KEY (CustomerId)
            REFERENCES Customers (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_ServiceOrders_Vehicles_VehicleId FOREIGN KEY (VehicleId)
            REFERENCES Vehicles (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_ServiceOrders_Users_CreatedBy FOREIGN KEY (CreatedBy)
            REFERENCES Users (Id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_ServiceOrders_CustomerId ON ServiceOrders (CustomerId);
    CREATE INDEX IX_ServiceOrders_VehicleId ON ServiceOrders (VehicleId);
    CREATE INDEX IX_ServiceOrders_CreatedBy ON ServiceOrders (CreatedBy);
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'ServiceOrderParts' AND xtype = 'U')
BEGIN
    CREATE TABLE ServiceOrderParts (
        Id              UNIQUEIDENTIFIER NOT NULL,
        ServiceOrderId  UNIQUEIDENTIFIER NOT NULL,
        InventoryItemId UNIQUEIDENTIFIER NOT NULL,
        Name            NVARCHAR(200)    NOT NULL,
        Description     NVARCHAR(1000)   NOT NULL,
        UnitPrice       DECIMAL(18,2)    NOT NULL,
        Quantity        INT              NOT NULL,
        CreatedAt       DATETIME2        NOT NULL,
        UpdatedAt       DATETIME2        NOT NULL,
        CONSTRAINT PK_ServiceOrderParts PRIMARY KEY (Id),
        CONSTRAINT FK_ServiceOrderParts_ServiceOrders_ServiceOrderId FOREIGN KEY (ServiceOrderId)
            REFERENCES ServiceOrders (Id) ON DELETE CASCADE,
        CONSTRAINT FK_ServiceOrderParts_InventoryItems_InventoryItemId FOREIGN KEY (InventoryItemId)
            REFERENCES InventoryItems (Id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_ServiceOrderParts_ServiceOrderId ON ServiceOrderParts (ServiceOrderId);
    CREATE INDEX IX_ServiceOrderParts_InventoryItemId ON ServiceOrderParts (InventoryItemId);
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'ServiceOrderServices' AND xtype = 'U')
BEGIN
    CREATE TABLE ServiceOrderServices (
        Id                UNIQUEIDENTIFIER NOT NULL,
        ServiceOrderId    UNIQUEIDENTIFIER NOT NULL,
        ServiceId         UNIQUEIDENTIFIER NOT NULL,
        Name              NVARCHAR(200)    NOT NULL,
        Description       NVARCHAR(1000)   NOT NULL,
        UnitPrice         DECIMAL(18,2)    NOT NULL,
        Quantity          INT              NOT NULL,
        EstimatedDuration SMALLINT         NOT NULL,
        CreatedAt         DATETIME2        NOT NULL,
        UpdatedAt         DATETIME2        NOT NULL,
        CONSTRAINT PK_ServiceOrderServices PRIMARY KEY (Id),
        CONSTRAINT FK_ServiceOrderServices_ServiceOrders_ServiceOrderId FOREIGN KEY (ServiceOrderId)
            REFERENCES ServiceOrders (Id) ON DELETE CASCADE,
        CONSTRAINT FK_ServiceOrderServices_Services_ServiceId FOREIGN KEY (ServiceId)
            REFERENCES Services (Id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_ServiceOrderServices_ServiceOrderId ON ServiceOrderServices (ServiceOrderId);
    CREATE INDEX IX_ServiceOrderServices_ServiceId ON ServiceOrderServices (ServiceId);
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'ServiceOrderStatusHistories' AND xtype = 'U')
BEGIN
    CREATE TABLE ServiceOrderStatusHistories (
        Id              UNIQUEIDENTIFIER NOT NULL,
        ServiceOrderId  UNIQUEIDENTIFIER NOT NULL,
        PreviousStatus  NVARCHAR(20)     NOT NULL,
        CurrentStatus   NVARCHAR(20)     NOT NULL,
        ChangedBy       UNIQUEIDENTIFIER NOT NULL,
        ChangedAt       DATETIME2        NOT NULL,
        CONSTRAINT PK_ServiceOrderStatusHistories PRIMARY KEY (Id),
        CONSTRAINT FK_ServiceOrderStatusHistories_ServiceOrders_ServiceOrderId FOREIGN KEY (ServiceOrderId)
            REFERENCES ServiceOrders (Id) ON DELETE CASCADE,
        CONSTRAINT FK_ServiceOrderStatusHistories_Users_ChangedBy FOREIGN KEY (ChangedBy)
            REFERENCES Users (Id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_ServiceOrderStatusHistories_ServiceOrderId ON ServiceOrderStatusHistories (ServiceOrderId);
    CREATE INDEX IX_ServiceOrderStatusHistories_ChangedBy ON ServiceOrderStatusHistories (ChangedBy);
END
GO
