IF DB_ID('SistemaAnalisisOpiniones') IS NULL
BEGIN
    CREATE DATABASE SistemaAnalisisOpiniones;
END
GO

USE SistemaAnalisisOpiniones;
GO

IF OBJECT_ID('dbo.Clientes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes (
        IdCliente  VARCHAR(20)  NOT NULL PRIMARY KEY,
        Nombre     VARCHAR(100) NOT NULL,
        Email      VARCHAR(100) NOT NULL
    );
END
GO

IF OBJECT_ID('dbo.Productos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Productos (
        IdProducto VARCHAR(20)  NOT NULL PRIMARY KEY,
        Nombre     VARCHAR(100) NOT NULL,
        Categoria  VARCHAR(50)  NULL
    );
END
GO

IF OBJECT_ID('dbo.FuenteDatos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FuenteDatos (
        IdFuente   VARCHAR(20) NOT NULL PRIMARY KEY,
        TipoFuente VARCHAR(50) NOT NULL,
        FechaCarga DATE        NOT NULL
    );
END
GO

IF OBJECT_ID('dbo.Encuestas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Encuestas (
        IdOpinion           INT            NOT NULL PRIMARY KEY,
        IdCliente           VARCHAR(20)    NOT NULL,
        IdProducto          VARCHAR(20)    NOT NULL,
        Fecha               DATE           NOT NULL,
        Comentario          NVARCHAR(1000) NOT NULL,
        Clasificacion       VARCHAR(20)    NOT NULL,
        PuntajeSatisfaccion INT            NOT NULL,
        Fuente              VARCHAR(50)    NOT NULL,
        CONSTRAINT FK_Encuestas_Cliente  FOREIGN KEY (IdCliente)  REFERENCES dbo.Clientes(IdCliente),
        CONSTRAINT FK_Encuestas_Producto FOREIGN KEY (IdProducto) REFERENCES dbo.Productos(IdProducto),
        CONSTRAINT CK_Encuestas_Clasificacion CHECK (Clasificacion IN ('Positiva', 'Negativa', 'Neutra')),
        CONSTRAINT CK_Encuestas_Puntaje       CHECK (PuntajeSatisfaccion BETWEEN 1 AND 5)
    );
END
GO

IF OBJECT_ID('dbo.ResenasWeb', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ResenasWeb (
        IdReview   VARCHAR(20)    NOT NULL PRIMARY KEY,
        IdCliente  VARCHAR(20)    NOT NULL,
        IdProducto VARCHAR(20)    NOT NULL,
        Fecha      DATE           NOT NULL,
        Comentario NVARCHAR(1000) NOT NULL,
        Rating     INT            NOT NULL,
        CONSTRAINT FK_ResenasWeb_Cliente  FOREIGN KEY (IdCliente)  REFERENCES dbo.Clientes(IdCliente),
        CONSTRAINT FK_ResenasWeb_Producto FOREIGN KEY (IdProducto) REFERENCES dbo.Productos(IdProducto),
        CONSTRAINT CK_ResenasWeb_Rating CHECK (Rating BETWEEN 1 AND 5)
    );
END
GO

IF OBJECT_ID('dbo.ComentariosSociales', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ComentariosSociales (
        IdComment  VARCHAR(20)    NOT NULL PRIMARY KEY,
        IdCliente  VARCHAR(20)    NULL,
        IdProducto VARCHAR(20)    NOT NULL,
        Fuente     VARCHAR(50)    NOT NULL,
        Fecha      DATE           NOT NULL,
        Comentario NVARCHAR(1000) NOT NULL,
        CONSTRAINT FK_ComentariosSociales_Cliente  FOREIGN KEY (IdCliente)  REFERENCES dbo.Clientes(IdCliente),
        CONSTRAINT FK_ComentariosSociales_Producto FOREIGN KEY (IdProducto) REFERENCES dbo.Productos(IdProducto)
    );
END
GO

CREATE INDEX IX_Encuestas_Producto           ON dbo.Encuestas(IdProducto);
CREATE INDEX IX_ResenasWeb_Producto          ON dbo.ResenasWeb(IdProducto);
CREATE INDEX IX_ComentariosSociales_Producto ON dbo.ComentariosSociales(IdProducto);
GO
