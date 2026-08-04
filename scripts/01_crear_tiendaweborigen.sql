-- =============================================================
-- Script: 01_crear_tiendaweborigen.sql
-- Crea la base de datos origen TiendaWebOrigen con la tabla de
-- resenas del sitio web. Esta BD simula el sistema transaccional
-- de la tienda del cual el proceso ETL extrae las resenas.
--
-- Nota: Fecha y Rating se almacenan como texto porque el sistema
-- origen no garantiza datos limpios; la validacion y conversion
-- de tipos es responsabilidad de la fase de transformacion del ETL.
-- =============================================================

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
