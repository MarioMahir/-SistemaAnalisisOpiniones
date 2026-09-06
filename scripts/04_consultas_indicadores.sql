USE SistemaAnalisisOpiniones_DW;
GO

-- 1. Total de opiniones procesadas y promedio de satisfaccion general
SELECT
    COUNT(*)                                        AS TotalOpiniones,
    CAST(AVG(CAST(PuntajeSatisfaccion AS DECIMAL(4,2))) AS DECIMAL(4,2)) AS PromedioSatisfaccion,
    COUNT(PuntajeSatisfaccion)                      AS OpinionesConPuntaje
FROM Fact_Opinion;
GO

-- 2. Opiniones por fuente (canal) y porcentaje del total
SELECT
    f.TipoFuente,
    COUNT(*)                                                        AS Opiniones,
    CAST(100.0 * COUNT(*) / SUM(COUNT(*)) OVER () AS DECIMAL(5,2))  AS Porcentaje
FROM Fact_Opinion o
JOIN Dim_Fuente f ON f.IdFuenteDim = o.IdFuenteDim
GROUP BY f.TipoFuente
ORDER BY Opiniones DESC;
GO

-- 3. Clasificacion de opiniones (positivas, negativas, neutras) y porcentaje
SELECT
    s.Clasificacion,
    COUNT(*)                                                        AS Opiniones,
    CAST(100.0 * COUNT(*) / SUM(COUNT(*)) OVER () AS DECIMAL(5,2))  AS Porcentaje
FROM Fact_Opinion o
JOIN Dim_Sentimiento s ON s.IdSentimientoDim = o.IdSentimientoDim
GROUP BY s.Clasificacion
ORDER BY Opiniones DESC;
GO

-- 4. Tono de las opiniones por canal: que canal concentra mas comentarios negativos
SELECT
    f.TipoFuente,
    SUM(CASE WHEN s.Clasificacion = 'Positiva' THEN 1 ELSE 0 END) AS Positivas,
    SUM(CASE WHEN s.Clasificacion = 'Negativa' THEN 1 ELSE 0 END) AS Negativas,
    SUM(CASE WHEN s.Clasificacion = 'Neutra'   THEN 1 ELSE 0 END) AS Neutras,
    CAST(100.0 * SUM(CASE WHEN s.Clasificacion = 'Negativa' THEN 1 ELSE 0 END) / COUNT(*) AS DECIMAL(5,2)) AS PorcentajeNegativas
FROM Fact_Opinion o
JOIN Dim_Fuente f      ON f.IdFuenteDim      = o.IdFuenteDim
JOIN Dim_Sentimiento s ON s.IdSentimientoDim = o.IdSentimientoDim
GROUP BY f.TipoFuente
ORDER BY PorcentajeNegativas DESC;
GO

-- 5. Productos con mas opiniones y su porcentaje de satisfaccion
--    (satisfaccion = opiniones positivas sobre el total del producto)
SELECT TOP 10
    p.IdProductoOrigen,
    p.Nombre,
    p.Categoria,
    COUNT(*)                                                                                   AS Opiniones,
    CAST(AVG(CAST(o.PuntajeSatisfaccion AS DECIMAL(4,2))) AS DECIMAL(4,2))                     AS PuntajePromedio,
    CAST(100.0 * SUM(CASE WHEN s.Clasificacion = 'Positiva' THEN 1 ELSE 0 END) / COUNT(*) AS DECIMAL(5,2)) AS PorcentajeSatisfaccion,
    SUM(CASE WHEN s.Clasificacion = 'Negativa' THEN 1 ELSE 0 END)                              AS Negativas
FROM Fact_Opinion o
JOIN Dim_Producto p    ON p.IdProductoDim    = o.IdProductoDim
JOIN Dim_Sentimiento s ON s.IdSentimientoDim = o.IdSentimientoDim
GROUP BY p.IdProductoOrigen, p.Nombre, p.Categoria
ORDER BY Opiniones DESC, PorcentajeSatisfaccion DESC;
GO

-- 6. Productos con mas opiniones negativas
SELECT TOP 10
    p.IdProductoOrigen,
    p.Nombre,
    SUM(CASE WHEN s.Clasificacion = 'Negativa' THEN 1 ELSE 0 END) AS Negativas,
    COUNT(*)                                                       AS Opiniones
FROM Fact_Opinion o
JOIN Dim_Producto p    ON p.IdProductoDim    = o.IdProductoDim
JOIN Dim_Sentimiento s ON s.IdSentimientoDim = o.IdSentimientoDim
GROUP BY p.IdProductoOrigen, p.Nombre
HAVING SUM(CASE WHEN s.Clasificacion = 'Negativa' THEN 1 ELSE 0 END) > 0
ORDER BY Negativas DESC, Opiniones DESC;
GO

-- 7. Tendencia de satisfaccion mes a mes
SELECT
    d.Anio,
    d.Mes,
    d.NombreMes,
    COUNT(*)                                                                                   AS Opiniones,
    CAST(AVG(CAST(o.PuntajeSatisfaccion AS DECIMAL(4,2))) AS DECIMAL(4,2))                     AS PuntajePromedio,
    CAST(100.0 * SUM(CASE WHEN s.Clasificacion = 'Positiva' THEN 1 ELSE 0 END) / COUNT(*) AS DECIMAL(5,2)) AS PorcentajePositivas,
    CAST(100.0 * SUM(CASE WHEN s.Clasificacion = 'Negativa' THEN 1 ELSE 0 END) / COUNT(*) AS DECIMAL(5,2)) AS PorcentajeNegativas
FROM Fact_Opinion o
JOIN Dim_Fecha d       ON d.IdFechaDim       = o.IdFechaDim
JOIN Dim_Sentimiento s ON s.IdSentimientoDim = o.IdSentimientoDim
GROUP BY d.Anio, d.Mes, d.NombreMes
ORDER BY d.Anio, d.Mes;
GO

-- 8. Tendencia trimestral
SELECT
    d.Anio,
    d.Trimestre,
    COUNT(*)                                                                                   AS Opiniones,
    CAST(100.0 * SUM(CASE WHEN s.Clasificacion = 'Positiva' THEN 1 ELSE 0 END) / COUNT(*) AS DECIMAL(5,2)) AS PorcentajePositivas
FROM Fact_Opinion o
JOIN Dim_Fecha d       ON d.IdFechaDim       = o.IdFechaDim
JOIN Dim_Sentimiento s ON s.IdSentimientoDim = o.IdSentimientoDim
GROUP BY d.Anio, d.Trimestre
ORDER BY d.Anio, d.Trimestre;
GO

-- 9. Opiniones de un producto en un rango de fechas (parametros de ejemplo)
DECLARE @IdProducto VARCHAR(20) = '16';
DECLARE @Desde DATE = '2024-10-01';
DECLARE @Hasta DATE = '2025-12-31';

SELECT
    d.Fecha,
    f.TipoFuente,
    c.Nombre         AS Cliente,
    s.Clasificacion,
    o.PuntajeSatisfaccion,
    o.Comentario
FROM Fact_Opinion o
JOIN Dim_Producto p    ON p.IdProductoDim    = o.IdProductoDim
JOIN Dim_Fecha d       ON d.IdFechaDim       = o.IdFechaDim
JOIN Dim_Fuente f      ON f.IdFuenteDim      = o.IdFuenteDim
JOIN Dim_Sentimiento s ON s.IdSentimientoDim = o.IdSentimientoDim
LEFT JOIN Dim_Cliente c ON c.IdClienteDim    = o.IdClienteDim
WHERE p.IdProductoOrigen = @IdProducto
  AND d.Fecha BETWEEN @Desde AND @Hasta
ORDER BY d.Fecha;
GO

-- 10. Clientes que mas opinan
SELECT TOP 10
    c.IdClienteOrigen,
    c.Nombre,
    COUNT(*)                                                       AS Opiniones,
    SUM(CASE WHEN s.Clasificacion = 'Positiva' THEN 1 ELSE 0 END) AS Positivas,
    SUM(CASE WHEN s.Clasificacion = 'Negativa' THEN 1 ELSE 0 END) AS Negativas
FROM Fact_Opinion o
JOIN Dim_Cliente c     ON c.IdClienteDim     = o.IdClienteDim
JOIN Dim_Sentimiento s ON s.IdSentimientoDim = o.IdSentimientoDim
GROUP BY c.IdClienteOrigen, c.Nombre
ORDER BY Opiniones DESC;
GO
