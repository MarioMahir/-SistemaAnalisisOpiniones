using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SistemaAnalisisOpiniones.Domain.Dtos;
using SistemaAnalisisOpiniones.Domain.Models;

namespace SistemaAnalisisOpiniones.Infrastructure.Loaders;

public class ComentarioSocialLoader : StagingLoaderBase<ComentarioSocialDto>
{
    protected override string TableName => "ComentariosSociales";

    public ComentarioSocialLoader(ILogger<ComentarioSocialLoader> logger) : base(logger) { }

    protected override async Task PreloadAsync(SqlConnection connection, EtlContext context, HashSet<string> seenKeys, CancellationToken ct)
    {
        var existing = await LoadExistingIdsAsync(connection, "SELECT IdComment FROM ComentariosSociales", ct);
        foreach (var id in existing) seenKeys.Add(id);
    }

    protected override async Task ProcessRowAsync(
        ComentarioSocialDto record, int rowNumber, SqlConnection connection, EtlContext context,
        HashSet<string> seenKeys, EtlResult result, CancellationToken ct)
    {
        var id = record.IdComment?.Trim();
        var idClienteRaw = Validation.NormalizarIdOrigen(record.IdCliente);
        var idCliente = Validation.IsRequired(idClienteRaw) ? idClienteRaw : null;
        var idProducto = Validation.NormalizarIdOrigen(record.IdProducto);
        var fuente = record.Fuente?.Trim();
        var comentario = record.Comentario?.Trim();

        if (!Validation.IsRequired(id) || !Validation.IsRequired(idProducto) ||
            !Validation.IsRequired(fuente) || !Validation.IsRequired(comentario) || !Validation.IsRequired(record.Fecha))
        {
            Reject(result, rowNumber, id, "Campos obligatorios faltantes (IdComment/IdProducto/Fuente/Fecha/Comentario)");
            return;
        }

        if (!Validation.TryParseDate(record.Fecha, out var fecha))
        {
            Reject(result, rowNumber, id, $"Formato de fecha inválido: '{record.Fecha}'");
            return;
        }

        if (!Validation.FitsLength(id, 20) || !Validation.FitsLength(idCliente, 20) ||
            !Validation.FitsLength(idProducto, 20) || !Validation.FitsLength(fuente, 50) ||
            !Validation.FitsLength(comentario, 1000))
        {
            Reject(result, rowNumber, id, "Algún campo excede la longitud máxima permitida");
            return;
        }

        if (idCliente is not null && !context.ClienteIds.Contains(idCliente))
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
            Reject(result, rowNumber, id, "IdComment duplicado (ya existe en el archivo o en la base de datos)");
            return;
        }

        await InsertAsync(
            connection,
            @"INSERT INTO ComentariosSociales (IdComment, IdCliente, IdProducto, Fuente, Fecha, Comentario)
              VALUES (@IdComment, @IdCliente, @IdProducto, @Fuente, @Fecha, @Comentario)",
            p =>
            {
                p.AddWithValue("@IdComment", id);
                p.AddWithValue("@IdCliente", (object?)idCliente ?? DBNull.Value);
                p.AddWithValue("@IdProducto", idProducto);
                p.AddWithValue("@Fuente", fuente);
                p.AddWithValue("@Fecha", fecha.Date);
                p.AddWithValue("@Comentario", comentario);
            },
            rowNumber, id, result, ct);
    }
}
