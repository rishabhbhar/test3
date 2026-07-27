-- Database: auth_db  (SQL Server)
-- ============================
IF DB_ID('auth_db') IS NULL
BEGIN
    CREATE DATABASE auth_db;
END
GO

USE auth_db;
GO

-- ============================
-- Table: users
-- ============================
IF OBJECT_ID('dbo.users', 'U') IS NULL
BEGIN
    CREATE TABLE users (
        user_id        UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        username       VARCHAR(100) NOT NULL,
        password_hash  VARCHAR(MAX) NOT NULL,
        role           VARCHAR(30)  NOT NULL,
        is_active      BIT NOT NULL DEFAULT 1,
        created_at     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT uq_users_username UNIQUE (username)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'chk_user_role')
BEGIN
    ALTER TABLE users
    ADD CONSTRAINT chk_user_role
    CHECK (role IN ('ADMIN', 'USER'));
END
GO
-- Database: inventory_db  (SQL Server)
-- ============================
IF DB_ID('inventory_db') IS NULL
BEGIN
    CREATE DATABASE inventory_db;
END
GO

USE inventory_db;
GO

-- ============================
-- Table: products
-- ============================
IF OBJECT_ID('dbo.products', 'U') IS NULL
BEGIN
    CREATE TABLE products (
        product_id     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        product_name   VARCHAR(150) NOT NULL,
        stock_qty      INT NOT NULL,
        is_active      BIT NOT NULL DEFAULT 1,
        created_at     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        updated_at     DATETIME2 NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'chk_stock_qty_non_negative')
BEGIN
    ALTER TABLE products
    ADD CONSTRAINT chk_stock_qty_non_negative
    CHECK (stock_qty >= 0);
END
GO
-- Database: order_db  (SQL Server)
-- ============================
IF DB_ID('order_db') IS NULL
BEGIN
    CREATE DATABASE order_db;
END
GO

USE order_db;
GO

-- ============================
-- Table: orders
-- ============================
IF OBJECT_ID('dbo.orders', 'U') IS NULL
BEGIN
    CREATE TABLE orders (
        order_id       UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        user_id        UNIQUEIDENTIFIER NOT NULL,
        order_status   VARCHAR(30) NOT NULL,
        created_at     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'chk_order_status')
BEGIN
    ALTER TABLE orders
    ADD CONSTRAINT chk_order_status
    CHECK (order_status IN ('CREATED', 'CONFIRMED', 'CANCELLED'));
END
GO

-- ============================
-- Table: order_items
-- ============================
IF OBJECT_ID('dbo.order_items', 'U') IS NULL
BEGIN
    CREATE TABLE order_items (
        order_item_id  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        order_id       UNIQUEIDENTIFIER NOT NULL,
        product_id     UNIQUEIDENTIFIER NOT NULL,
        quantity       INT NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'chk_order_item_quantity')
BEGIN
    ALTER TABLE order_items
    ADD CONSTRAINT chk_order_item_quantity
    CHECK (quantity > 0);
END
GO

-- ============================
-- Foreign Key (Same Service Only)
-- ============================
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'fk_order_items_order')
BEGIN
    ALTER TABLE order_items
    ADD CONSTRAINT fk_order_items_order
    FOREIGN KEY (order_id)
    REFERENCES orders(order_id)
    ON DELETE CASCADE;
END
GO
