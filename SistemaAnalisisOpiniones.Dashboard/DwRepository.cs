using Microsoft.Data.SqlClient;

namespace SistemaAnalisisOpiniones.Dashboard;

// Consultas de solo lectura sobre el Data Warehouse (modelo estrella). Cada método
// devuelve filas como diccionarios para serializarlas directo a JSON.
public class DwRepository
{
    private readonly string _connectionString;

    public DwRepository(IConfiguration configuration)
    {
        _connectionString = configuration["Dw:ConnectionString"]
            ?? throw new InvalidOperationException("Falta Dw:ConnectionString en la configuración.");
    }

    public Task<List<Dictionary<string, object?>>> ResumenAsync(CancellationToken ct) => QueryAsync(@"
        SELECT
            COUNT(*) AS TotalOpiniones,
            CAST(AVG(CAST(o.PuntajeSatisfaccion AS DECIMAL(4,2))) AS DECIMAL(4,2)) AS PromedioSatisfaccion,
            CAST(100.0 * SUM(CASE WHEN s.Clasificacion = 'Positiva' THEN 1 ELSE 0 END) / COUNT(*) AS DECIMAL(5,2)) AS PorcentajePositivas,
            CAST(100.0 * SUM(CASE WHEN s.Clasificacion = 'Negativa' THEN 1 ELSE 0 END) / COUNT(*) AS DECIMAL(5,2)) AS PorcentajeNegativas,
            (SELECT COUNT(DISTINCT IdProductoDim) FROM Fact_Opinion) AS ProductosConOpiniones,
            (SELECT COUNT(DISTINCT IdClienteDim) FROM Fact_Opinion WHERE IdClienteDim IS NOT NULL) AS ClientesQueOpinan
        FROM Fact_Opinion o
        JOIN Dim_Sentimiento s ON s.IdSentimientoDim = o.IdSentimientoDim", ct);

    public Task<List<Dictionary<string, object?>>> SentimientosAsync(CancellationToken ct) => QueryAsync(@"
        SELECT s.Clasificacion, COUNT(*) AS Opiniones
        FROM Fact_Opinion o
        JOIN Dim_Sentimiento s ON s.IdSentimientoDim = o.IdSentimientoDim
        GROUP BY s.Clasificacion
        ORDER BY CASE s.Clasificacion WHEN 'Positiva' THEN 1 WHEN 'Neutra' THEN 2 ELSE 3 END", ct);

    public Task<List<Dictionary<string, object?>>> FuentesAsync(CancellationToken ct) => QueryAsync(@"
        SELECT f.TipoFuente,
               SUM(CASE WHEN s.Clasificacion = 'Positiva' THEN 1 ELSE 0 END) AS Positivas,
               SUM(CASE WHEN s.Clasificacion = 'Neutra'   THEN 1 ELSE 0 END) AS Neutras,
               SUM(CASE WHEN s.Clasificacion = 'Negativa' THEN 1 ELSE 0 END) AS Negativas,
               COUNT(*) AS Opiniones
        FROM Fact_Opinion o
        JOIN Dim_Fuente f      ON f.IdFuenteDim      = o.IdFuenteDim
        JOIN Dim_Sentimiento s ON s.IdSentimientoDim = o.IdSentimientoDim
        GROUP BY f.TipoFuente
        ORDER BY Opiniones DESC", ct);

    public Task<List<Dictionary<string, object?>>> TendenciaMensualAsync(CancellationToken ct) => QueryAsync(@"
        SELECT d.Anio, d.Mes, d.NombreMes,
               COUNT(*) AS Opiniones,
               CAST(AVG(CAST(o.PuntajeSatisfaccion AS DECIMAL(4,2))) AS DECIMAL(4,2)) AS PuntajePromedio,
               CAST(100.0 * SUM(CASE WHEN s.Clasificacion = 'Positiva' THEN 1 ELSE 0 END) / COUNT(*) AS DECIMAL(5,2)) AS PorcentajePositivas,
               CAST(100.0 * SUM(CASE WHEN s.Clasificacion = 'Negativa' THEN 1 ELSE 0 END) / COUNT(*) AS DECIMAL(5,2)) AS PorcentajeNegativas
        FROM Fact_Opinion o
        JOIN Dim_Fecha d       ON d.IdFechaDim       = o.IdFechaDim
        JOIN Dim_Sentimiento s ON s.IdSentimientoDim = o.IdSentimientoDim
        GROUP BY d.Anio, d.Mes, d.NombreMes
        ORDER BY d.Anio, d.Mes", ct);

    public Task<List<Dictionary<string, object?>>> ProductosAsync(int top, CancellationToken ct) => QueryAsync(@"
        SELECT TOP (@Top) p.IdProductoOrigen, p.Nombre, p.Categoria,
               COUNT(*) AS Opiniones,
               CAST(AVG(CAST(o.PuntajeSatisfaccion AS DECIMAL(4,2))) AS DECIMAL(4,2)) AS PuntajePromedio,
               CAST(100.0 * SUM(CASE WHEN s.Clasificacion = 'Positiva' THEN 1 ELSE 0 END) / COUNT(*) AS DECIMAL(5,2)) AS PorcentajeSatisfaccion,
               SUM(CASE WHEN s.Clasificacion = 'Negativa' THEN 1 ELSE 0 END) AS Negativas
        FROM Fact_Opinion o
        JOIN Dim_Producto p    ON p.IdProductoDim    = o.IdProductoDim
        JOIN Dim_Sentimiento s ON s.IdSentimientoDim = o.IdSentimientoDim
        GROUP BY p.IdProductoOrigen, p.Nombre, p.Categoria
        ORDER BY Opiniones DESC, PorcentajeSatisfaccion DESC",
        ct, ("@Top", top));

    public Task<List<Dictionary<string, object?>>> ListaProductosAsync(CancellationToken ct) => QueryAsync(@"
        SELECT p.IdProductoOrigen, p.Nombre, COUNT(*) AS Opiniones
        FROM Fact_Opinion o
        JOIN Dim_Producto p ON p.IdProductoDim = o.IdProductoDim
        GROUP BY p.IdProductoOrigen, p.Nombre
        ORDER BY Opiniones DESC, p.Nombre", ct);

    public Task<List<Dictionary<string, object?>>> TendenciaProductoAsync(string idProducto, DateTime desde, DateTime hasta, CancellationToken ct) => QueryAsync(@"
        SELECT d.Anio, d.Mes, d.NombreMes,
               COUNT(*) AS Opiniones,
               CAST(AVG(CAST(o.PuntajeSatisfaccion AS DECIMAL(4,2))) AS DECIMAL(4,2)) AS PuntajePromedio,
               CAST(100.0 * SUM(CASE WHEN s.Clasificacion = 'Positiva' THEN 1 ELSE 0 END) / COUNT(*) AS DECIMAL(5,2)) AS PorcentajePositivas
        FROM Fact_Opinion o
        JOIN Dim_Producto p    ON p.IdProductoDim    = o.IdProductoDim
        JOIN Dim_Fecha d       ON d.IdFechaDim       = o.IdFechaDim
        JOIN Dim_Sentimiento s ON s.IdSentimientoDim = o.IdSentimientoDim
        WHERE p.IdProductoOrigen = @IdProducto AND d.Fecha BETWEEN @Desde AND @Hasta
        GROUP BY d.Anio, d.Mes, d.NombreMes
        ORDER BY d.Anio, d.Mes",
        ct, ("@IdProducto", idProducto), ("@Desde", desde.Date), ("@Hasta", hasta.Date));

    public Task<List<Dictionary<string, object?>>> OpinionesAsync(string idProducto, DateTime desde, DateTime hasta, CancellationToken ct) => QueryAsync(@"
        SELECT d.Fecha, f.TipoFuente, c.Nombre AS Cliente, s.Clasificacion, o.PuntajeSatisfaccion, o.Comentario
        FROM Fact_Opinion o
        JOIN Dim_Producto p     ON p.IdProductoDim    = o.IdProductoDim
        JOIN Dim_Fecha d        ON d.IdFechaDim       = o.IdFechaDim
        JOIN Dim_Fuente f       ON f.IdFuenteDim      = o.IdFuenteDim
        JOIN Dim_Sentimiento s  ON s.IdSentimientoDim = o.IdSentimientoDim
        LEFT JOIN Dim_Cliente c ON c.IdClienteDim     = o.IdClienteDim
        WHERE p.IdProductoOrigen = @IdProducto AND d.Fecha BETWEEN @Desde AND @Hasta
        ORDER BY d.Fecha DESC",
        ct, ("@IdProducto", idProducto), ("@Desde", desde.Date), ("@Hasta", hasta.Date));

    private async Task<List<Dictionary<string, object?>>> QueryAsync(
        string sql, CancellationToken ct, params (string Nombre, object Valor)[] parametros)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        foreach (var (nombre, valor) in parametros)
            command.Parameters.AddWithValue(nombre, valor);

        var filas = new List<Dictionary<string, object?>>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var fila = new Dictionary<string, object?>(reader.FieldCount);
            for (var i = 0; i < reader.FieldCount; i++)
                fila[reader.GetName(i)] = await reader.IsDBNullAsync(i, ct) ? null : reader.GetValue(i);
            filas.Add(fila);
        }
        return filas;
    }
}
