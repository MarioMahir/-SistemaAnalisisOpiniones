IF DB_ID('SistemaAnalisisOpiniones_DW') IS NULL
BEGIN
    CREATE DATABASE SistemaAnalisisOpiniones_DW;
END
GO

USE SistemaAnalisisOpiniones_DW;
GO

CREATE TABLE Dim_Fecha (
    IdFechaDim      INT             NOT NULL PRIMARY KEY, 
    Fecha           DATE            NOT NULL,
    Anio            INT             NOT NULL,
    Trimestre       TINYINT         NOT NULL,
    Mes             TINYINT         NOT NULL,
    NombreMes       VARCHAR(20)     NOT NULL,
    Dia             TINYINT         NOT NULL,
    DiaSemana       VARCHAR(15)     NOT NULL,
    CONSTRAINT UQ_Dim_Fecha_Fecha UNIQUE (Fecha)
);
GO

CREATE TABLE Dim_Cliente (
    IdClienteDim    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdClienteOrigen VARCHAR(20)     NOT NULL,             
    Nombre          VARCHAR(100)    NOT NULL,
    Email           VARCHAR(100)    NULL,
    Pais            VARCHAR(60)     NULL,
    RangoEdad       VARCHAR(20)     NULL,                  
    TipoCliente     VARCHAR(30)     NULL,                  
    CONSTRAINT UQ_Dim_Cliente_Origen UNIQUE (IdClienteOrigen)
);
GO

CREATE TABLE Dim_Producto (
    IdProductoDim    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdProductoOrigen VARCHAR(20)    NOT NULL,                
    Nombre           VARCHAR(100)   NOT NULL,
    Categoria        VARCHAR(50)    NULL,
    CONSTRAINT UQ_Dim_Producto_Origen UNIQUE (IdProductoOrigen)
);
GO

CREATE TABLE Dim_Fuente (
    IdFuenteDim     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TipoFuente      VARCHAR(20)     NOT NULL,              
    Descripcion     VARCHAR(100)    NULL,
    CONSTRAINT UQ_Dim_Fuente_Tipo UNIQUE (TipoFuente),
    CONSTRAINT CK_Dim_Fuente_Tipo CHECK (TipoFuente IN ('Encuesta','Web','RedSocial'))
);
GO

CREATE TABLE Dim_Sentimiento (
    IdSentimientoDim INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Clasificacion    VARCHAR(20)    NOT NULL,                
    CONSTRAINT UQ_Dim_Sentimiento_Clasificacion UNIQUE (Clasificacion),
    CONSTRAINT CK_Dim_Sentimiento_Clasificacion CHECK (Clasificacion IN ('Positiva','Negativa','Neutra'))
);
GO

CREATE TABLE Fact_Opinion (
    IdOpinionFact       BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdClienteDim        INT             NULL,                
    IdProductoDim       INT             NOT NULL,
    IdFechaDim          INT             NOT NULL,
    IdFuenteDim         INT             NOT NULL,
    IdSentimientoDim    INT             NOT NULL,
    PuntajeSatisfaccion INT             NULL,         
    Comentario          NVARCHAR(1000)  NULL,
    OrigenTipo          VARCHAR(20)     NOT NULL, 
    OrigenId            VARCHAR(20)     NOT NULL,          

    CONSTRAINT FK_Fact_Opinion_Cliente     FOREIGN KEY (IdClienteDim)     REFERENCES Dim_Cliente(IdClienteDim)         ON DELETE NO ACTION,
    CONSTRAINT FK_Fact_Opinion_Producto    FOREIGN KEY (IdProductoDim)    REFERENCES Dim_Producto(IdProductoDim)       ON DELETE NO ACTION,
    CONSTRAINT FK_Fact_Opinion_Fecha       FOREIGN KEY (IdFechaDim)       REFERENCES Dim_Fecha(IdFechaDim)             ON DELETE NO ACTION,
    CONSTRAINT FK_Fact_Opinion_Fuente      FOREIGN KEY (IdFuenteDim)      REFERENCES Dim_Fuente(IdFuenteDim)           ON DELETE NO ACTION,
    CONSTRAINT FK_Fact_Opinion_Sentimiento FOREIGN KEY (IdSentimientoDim) REFERENCES Dim_Sentimiento(IdSentimientoDim) ON DELETE NO ACTION,

    CONSTRAINT CK_Fact_Opinion_OrigenTipo      CHECK (OrigenTipo IN ('Encuesta','ResenaWeb','RedSocial')),
    CONSTRAINT CK_Fact_Opinion_Puntaje         CHECK (PuntajeSatisfaccion IS NULL OR PuntajeSatisfaccion BETWEEN 1 AND 5)
);
GO

CREATE INDEX IX_Fact_Opinion_Cliente     ON Fact_Opinion(IdClienteDim);
CREATE INDEX IX_Fact_Opinion_Producto    ON Fact_Opinion(IdProductoDim);
CREATE INDEX IX_Fact_Opinion_Fecha       ON Fact_Opinion(IdFechaDim);
CREATE INDEX IX_Fact_Opinion_Fuente      ON Fact_Opinion(IdFuenteDim);
CREATE INDEX IX_Fact_Opinion_Sentimiento ON Fact_Opinion(IdSentimientoDim);
GO

INSERT INTO Dim_Fuente (TipoFuente, Descripcion) VALUES
    ('Encuesta',  'Encuestas internas de satisfacción (CSV)'),
    ('Web',       'Reseñas publicadas en el sitio web (BD relacional)'),
    ('RedSocial', 'Comentarios de redes sociales (API REST)');
GO

INSERT INTO Dim_Sentimiento (Clasificacion) VALUES
    ('Positiva'),
    ('Negativa'),
    ('Neutra');
GO
