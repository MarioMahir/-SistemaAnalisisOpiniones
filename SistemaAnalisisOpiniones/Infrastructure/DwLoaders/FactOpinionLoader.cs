using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SistemaAnalisisOpiniones.Application.Interfaces;
using SistemaAnalisisOpiniones.Domain.Models;

namespace SistemaAnalisisOpiniones.Infrastructure.DwLoaders;

public class FactOpinionLoader
{
    private readonly ILogger<FactOpinionLoader> _logger;
    private readonly ISentimentClassifier _classifier;

    public FactOpinionLoader(ILogger<FactOpinionLoader> logger, ISentimentClassifier classifier)
    {
        _logger = logger;
        _classifier = classifier;
    }

    private Dictionary<string, int> _clientes = new();
    private Dictionary<string, int> _productos = new();
    private Dictionary<string, int> _fuentes = new();
    private Dictionary<string, int> _sentimientos = new();
    private HashSet<int> _fechas = new();

    public async Task<(long FilasEliminadas, long DuracionMs)> LimpiarAsync(SqlConnection dw, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        long filas;
        await using (var conteo = new SqlCommand("SELECT COUNT_BIG(*) FROM Fact_Opinion", dw))
            filas = (long)(await conteo.ExecuteScalarAsync(ct))!;

        await using (var truncate = new SqlCommand("TRUNCATE TABLE Fact_Opinion", dw))
            await truncate.ExecuteNonQueryAsync(ct);

        stopwatch.Stop();
        _logger.LogInformation(
            "Limpieza Fact_Opinion: {Filas} filas eliminadas ({DuracionMs} ms)",
            filas, stopwatch.ElapsedMilliseconds);

        return (filas, stopwatch.ElapsedMilliseconds);
    }

    public async Task PrecargarDimensionesAsync(SqlConnection dw, CancellationToken ct)
    {
        _clientes = await CargarMapaAsync(dw, "SELECT IdClienteOrigen, IdClienteDim FROM Dim_Cliente", ct);
        _productos = await CargarMapaAsync(dw, "SELECT IdProductoOrigen, IdProductoDim FROM Dim_Producto", ct);
        _fuentes = await CargarMapaAsync(dw, "SELECT TipoFuente, IdFuenteDim FROM Dim_Fuente", ct);
        _sentimientos = await CargarMapaAsync(dw, "SELECT Clasificacion, IdSentimientoDim FROM Dim_Sentimiento", ct);

        _fechas = new HashSet<int>();
        await using var command = new SqlCommand("SELECT IdFechaDim FROM Dim_Fecha", dw);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            _fechas.Add(reader.GetInt32(0));

        _logger.LogInformation(
            "Dimensiones precargadas: {Clientes} clientes, {Productos} productos, {Fechas} fechas, {Fuentes} fuentes, {Sentimientos} sentimientos",
            _clientes.Count, _productos.Count, _fechas.Count, _fuentes.Count, _sentimientos.Count);
    }

    public Task<ResultadoCargaFact> CargarEncuestasAsync(SqlConnection staging, SqlConnection dw, CancellationToken ct) =>
        CargarFuenteAsync("Encuestas", staging, dw, ct,
            "SELECT CAST(IdOpinion AS VARCHAR(20)), IdCliente, IdProducto, Fecha, Comentario, Clasificacion, PuntajeSatisfaccion FROM Encuestas",
            tipoFuente: "Encuesta", origenTipo: "Encuesta",
            (reader, fila) =>
            {
                fila.Puntaje = reader.GetInt32(6);
                fila.Sentimiento = reader.GetString(5).Trim();
            });

    public Task<ResultadoCargaFact> CargarResenasWebAsync(SqlConnection staging, SqlConnection dw, CancellationToken ct) =>
        CargarFuenteAsync("ResenasWeb", staging, dw, ct,
            "SELECT IdReview, IdCliente, IdProducto, Fecha, Comentario, Rating, NULL FROM ResenasWeb",
            tipoFuente: "Web", origenTipo: "ResenaWeb",
            (reader, fila) =>
            {
                var rating = reader.GetInt32(5);
                fila.Puntaje = rating;
                fila.Sentimiento = rating >= 4 ? "Positiva" : rating == 3 ? "Neutra" : "Negativa";
            });

    public Task<ResultadoCargaFact> CargarComentariosSocialesAsync(SqlConnection staging, SqlConnection dw, CancellationToken ct) =>
        CargarFuenteAsync("ComentariosSociales", staging, dw, ct,
            "SELECT IdComment, IdCliente, IdProducto, Fecha, Comentario, NULL, NULL FROM ComentariosSociales",
            tipoFuente: "RedSocial", origenTipo: "RedSocial",
            (reader, fila) =>
            {
                // Los comentarios sociales no traen puntaje ni clasificación: se clasifican
                // por palabras clave (transformación exigida por el SRS).
                fila.Puntaje = null;
                fila.Sentimiento = _classifier.Clasificar(fila.Comentario);
            });

    private sealed class FilaFact
    {
        public string OrigenId = "";
        public string? IdClienteOrigen;
        public string IdProductoOrigen = "";
        public DateTime Fecha;
        public string? Comentario;
        public int? Puntaje;
        public string Sentimiento = "";
    }

    private async Task<ResultadoCargaFact> CargarFuenteAsync(
        string nombreFuente, SqlConnection staging, SqlConnection dw, CancellationToken ct,
        string sqlLectura, string tipoFuente, string origenTipo,
        Action<SqlDataReader, FilaFact> transformar)
    {
        var resultado = new ResultadoCargaFact { Fuente = nombreFuente };
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!_fuentes.TryGetValue(tipoFuente, out var idFuenteDim))
                throw new InvalidOperationException($"Dim_Fuente no contiene el tipo '{tipoFuente}'.");

            var filas = new List<FilaFact>();
            await using (var lectura = new SqlCommand(sqlLectura, staging))
            await using (var reader = await lectura.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    resultado.Leidos++;
                    var fila = new FilaFact
                    {
                        OrigenId = Convert.ToString(reader.GetValue(0))!.Trim(),
                        IdClienteOrigen = await reader.IsDBNullAsync(1, ct) ? null : reader.GetString(1).Trim(),
                        IdProductoOrigen = reader.GetString(2).Trim(),
                        Fecha = reader.GetDateTime(3).Date,
                        Comentario = await reader.IsDBNullAsync(4, ct) ? null : reader.GetString(4),
                    };
                    transformar(reader, fila);
                    filas.Add(fila);
                }
            }

            foreach (var fila in filas)
            {
                if (!_productos.TryGetValue(fila.IdProductoOrigen, out var idProductoDim))
                {
                    Rechazar(resultado, fila.OrigenId, $"IdProducto '{fila.IdProductoOrigen}' no existe en Dim_Producto");
                    continue;
                }

                int? idClienteDim = null;
                if (fila.IdClienteOrigen is not null)
                {
                    if (!_clientes.TryGetValue(fila.IdClienteOrigen, out var idCliente))
                    {
                        Rechazar(resultado, fila.OrigenId, $"IdCliente '{fila.IdClienteOrigen}' no existe en Dim_Cliente");
                        continue;
                    }
                    idClienteDim = idCliente;
                }

                var idFechaDim = fila.Fecha.Year * 10000 + fila.Fecha.Month * 100 + fila.Fecha.Day;
                if (!_fechas.Contains(idFechaDim))
                {
                    Rechazar(resultado, fila.OrigenId, $"La fecha {fila.Fecha:yyyy-MM-dd} no existe en Dim_Fecha");
                    continue;
                }

                if (!_sentimientos.TryGetValue(fila.Sentimiento, out var idSentimientoDim))
                {
                    Rechazar(resultado, fila.OrigenId, $"Clasificación '{fila.Sentimiento}' no existe en Dim_Sentimiento");
                    continue;
                }

                await using var insert = new SqlCommand(
                    @"INSERT INTO Fact_Opinion
                          (IdClienteDim, IdProductoDim, IdFechaDim, IdFuenteDim, IdSentimientoDim,
                           PuntajeSatisfaccion, Comentario, OrigenTipo, OrigenId)
                      VALUES
                          (@IdClienteDim, @IdProductoDim, @IdFechaDim, @IdFuenteDim, @IdSentimientoDim,
                           @Puntaje, @Comentario, @OrigenTipo, @OrigenId)", dw);
                insert.Parameters.AddWithValue("@IdClienteDim", (object?)idClienteDim ?? DBNull.Value);
                insert.Parameters.AddWithValue("@IdProductoDim", idProductoDim);
                insert.Parameters.AddWithValue("@IdFechaDim", idFechaDim);
                insert.Parameters.AddWithValue("@IdFuenteDim", idFuenteDim);
                insert.Parameters.AddWithValue("@IdSentimientoDim", idSentimientoDim);
                insert.Parameters.AddWithValue("@Puntaje", (object?)fila.Puntaje ?? DBNull.Value);
                insert.Parameters.AddWithValue("@Comentario", (object?)fila.Comentario ?? DBNull.Value);
                insert.Parameters.AddWithValue("@OrigenTipo", origenTipo);
                insert.Parameters.AddWithValue("@OrigenId", fila.OrigenId);
                await insert.ExecuteNonQueryAsync(ct);
                resultado.Insertados++;
                resultado.PorSentimiento[fila.Sentimiento] = resultado.PorSentimiento.GetValueOrDefault(fila.Sentimiento) + 1;
            }

            resultado.Exitoso = true;
        }
        catch (Exception ex)
        {
            resultado.Exitoso = false;
            resultado.Error = ex.Message;
            _logger.LogError(ex, "Carga Fact_Opinion desde {Fuente}: falló", nombreFuente);
        }
        finally
        {
            stopwatch.Stop();
            resultado.DuracionMs = stopwatch.ElapsedMilliseconds;
        }

        if (resultado.Exitoso)
        {
            _logger.LogInformation(
                "Carga Fact_Opinion desde {Fuente}: {Leidos} leídos, {Insertados} insertados, {Rechazados} rechazados ({DuracionMs} ms)",
                nombreFuente, resultado.Leidos, resultado.Insertados, resultado.Rechazados, resultado.DuracionMs);
        }

        return resultado;
    }

    private static void Rechazar(ResultadoCargaFact resultado, string origenId, string motivo)
    {
        resultado.Rechazados++;
        resultado.MotivosRechazo.Add($"[{origenId}] {motivo}");
    }

    private static async Task<Dictionary<string, int>> CargarMapaAsync(
        SqlConnection dw, string sql, CancellationToken ct)
    {
        var mapa = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        await using var command = new SqlCommand(sql, dw);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            mapa[reader.GetString(0).Trim()] = reader.GetInt32(1);
        return mapa;
    }
}
