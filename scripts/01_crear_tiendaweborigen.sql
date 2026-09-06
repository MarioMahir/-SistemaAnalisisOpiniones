IF DB_ID('TiendaWebOrigen') IS NULL
BEGIN
    CREATE DATABASE TiendaWebOrigen;
END
GO

USE TiendaWebOrigen;
GO

IF OBJECT_ID('dbo.Resenas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Resenas (
        IdReview   VARCHAR(20)    NOT NULL PRIMARY KEY,
        IdCliente  VARCHAR(20)    NULL,
        IdProducto VARCHAR(20)    NULL,
        Fecha      VARCHAR(20)    NULL,
        Comentario NVARCHAR(1000) NULL,
        Rating     VARCHAR(10)    NULL
    );
END
GO
