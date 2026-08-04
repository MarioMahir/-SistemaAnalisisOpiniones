using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaAnalisisOpiniones.Csv;
using SistemaAnalisisOpiniones.Infrastructure;
using SistemaAnalisisOpiniones.Models;

namespace SistemaAnalisisOpiniones.Etl;

public class ResenaWebLoader : CsvLoaderBase<ResenaWebCsv>
{
    protected override string CsvFileName => "web_reviews.csv";
    protected override string TableName => "ResenasWeb";

    public ResenaWebLoader(IOptions<EtlOptions> options, ILogger<ResenaWebLoader> logger) : base(options, logger) { }

    protected override async Task PreloadAsync(SqlConnection connection, EtlContext context, HashSet<string> seenKeys, CancellationToken ct)
    {
        var existing = await LoadExistingIdsAsync(connection, "SELECT IdReview FROM ResenasWeb", ct);
        foreach (var id in existing) seenKeys.Add(id);
    }

    protected override async Task ProcessRowAsync(
        ResenaWebCsv record, int rowNumber, SqlConnection connection, EtlContext context,
        HashSet<string> seenKeys, EtlResult result, CancellationToken ct)
    {
        var id = record.IdReview?.Trim();
        var idCliente = record.IdCliente?.Trim();
        var idProducto = record.IdProducto?.Trim();
        var comentario = record.Comentario?.Trim();
        var ratingTexto = record.Rating?.Trim();

        if (!Validation.IsRequired(id) || !Validation.IsRequired(idCliente) || !Validation.IsRequired(idProducto) ||
            !Validation.IsRequired(comentario) || !Validation.IsRequired(ratingTexto) || !Validation.IsRequired(record.Fecha))
        {
            Reject(result, rowNumber, id, "Campos obligatorios faltantes en la reseña web");
            return;
        }

        if (!Validation.TryParseDate(record.Fecha, out var fecha))
        {
            Reject(result, rowNumber, id, $"Formato de fecha inválido: '{record.Fecha}'");
            return;
        }

        if (!Validation.TryParseInt(ratingTexto, out var rating) || rating < 1 || rating > 5)
        {
            Reject(result, rowNumber, id, $"Rating inválido (debe ser 1-5): '{ratingTexto}'");
            return;
        }

        if (!Validation.FitsLength(id, 20) || !Validation.FitsLength(idCliente, 20) ||
            !Validation.FitsLength(idProducto, 20) || !Validation.FitsLength(comentario, 1000))
        {
            Reject(result, rowNumber, id, "Algún campo excede la longitud máxima permitida");
            return;
        }

        if (!context.ClienteIds.Contains(idCliente!))
        {
            Reject(result, rowNumber, id, $"IdCliente '{idCliente}' no existe en Clientes (integridad referencial)");
            return;
        }

        if (!context.ProductoIds.Contains(idProducto!))
        {
            Reject(result, rowNumber, id, $"IdProducto '{idProducto}' no existe en Productos (integridad referencial)");
            return;
        }

        if (!seenKeys.Add(id!))
        {
            Reject(result, rowNumber, id, "IdReview duplicado (ya existe en el archivo o en la base de datos)");
            return;
        }

        await InsertAsync(
            connection,
            @"INSERT INTO ResenasWeb (IdReview, IdCliente, IdProducto, Fecha, Comentario, Rating)
              VALUES (@IdReview, @IdCliente, @IdProducto, @Fecha, @Comentario, @Rating)",
            p =>
            {
                p.AddWithValue("@IdReview", id);
                p.AddWithValue("@IdCliente", idCliente);
                p.AddWithValue("@IdProducto", idProducto);
                p.AddWithValue("@Fecha", fecha.Date);
                p.AddWithValue("@Comentario", comentario);
                p.AddWithValue("@Rating", rating);
            },
            rowNumber, id, result, ct);
    }
}
