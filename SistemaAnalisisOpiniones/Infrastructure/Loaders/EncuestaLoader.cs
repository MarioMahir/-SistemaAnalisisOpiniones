using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SistemaAnalisisOpiniones.Domain.Dtos;
using SistemaAnalisisOpiniones.Domain.Models;

namespace SistemaAnalisisOpiniones.Infrastructure.Loaders;

public class EncuestaLoader : StagingLoaderBase<EncuestaDto>
{
    private static readonly HashSet<string> ClasificacionesValidas =
        new(StringComparer.OrdinalIgnoreCase) { "Positiva", "Negativa", "Neutra" };

    protected override string TableName => "Encuestas";

    public EncuestaLoader(ILogger<EncuestaLoader> logger) : base(logger) { }

    protected override async Task PreloadAsync(SqlConnection connection, EtlContext context, HashSet<string> seenKeys, CancellationToken ct)
    {
        var existing = await LoadExistingIdsAsync(connection, "SELECT CAST(IdOpinion AS VARCHAR(20)) FROM Encuestas", ct);
        foreach (var id in existing) seenKeys.Add(id);
    }

    protected override async Task ProcessRowAsync(
        EncuestaDto record, int rowNumber, SqlConnection connection, EtlContext context,
        HashSet<string> seenKeys, EtlResult result, CancellationToken ct)
    {
        var idOpinionTexto = record.IdOpinion?.Trim();
        var idCliente = record.IdCliente?.Trim();
        var idProducto = record.IdProducto?.Trim();
        var comentario = record.Comentario?.Trim();
        var clasificacion = record.Clasificacion?.Trim();
        var puntajeTexto = record.PuntajeSatisfaccion?.Trim();
        var fuente = record.Fuente?.Trim();

        if (!Validation.IsRequired(idOpinionTexto) || !Validation.IsRequired(idCliente) ||
            !Validation.IsRequired(idProducto) || !Validation.IsRequired(comentario) ||
            !Validation.IsRequired(clasificacion) || !Validation.IsRequired(puntajeTexto) ||
            !Validation.IsRequired(fuente) || !Validation.IsRequired(record.Fecha))
        {
            Reject(result, rowNumber, idOpinionTexto, "Campos obligatorios faltantes en la encuesta");
            return;
        }

        if (!Validation.TryParseInt(idOpinionTexto, out var idOpinion))
        {
            Reject(result, rowNumber, idOpinionTexto, $"IdOpinion no es un entero válido: '{idOpinionTexto}'");
            return;
        }

        if (!Validation.TryParseDate(record.Fecha, out var fecha))
        {
            Reject(result, rowNumber, idOpinionTexto, $"Formato de fecha inválido: '{record.Fecha}'");
            return;
        }

        if (!Validation.TryParseInt(puntajeTexto, out var puntaje) || puntaje < 1 || puntaje > 5)
        {
            Reject(result, rowNumber, idOpinionTexto, $"PuntajeSatisfaccion inválido (debe ser 1-5): '{puntajeTexto}'");
            return;
        }

        if (!ClasificacionesValidas.Contains(clasificacion!))
        {
            Reject(result, rowNumber, idOpinionTexto, $"Clasificacion inválida (Positiva/Negativa/Neutra): '{clasificacion}'");
            return;
        }

        if (!Validation.FitsLength(idCliente, 20) || !Validation.FitsLength(idProducto, 20) ||
            !Validation.FitsLength(clasificacion, 20) || !Validation.FitsLength(fuente, 50) ||
            !Validation.FitsLength(comentario, 1000))
        {
            Reject(result, rowNumber, idOpinionTexto, "Algún campo excede la longitud máxima permitida");
            return;
        }

        if (!context.ClienteIds.Contains(idCliente!))
        {
            Reject(result, rowNumber, idOpinionTexto, $"IdCliente '{idCliente}' no existe en Clientes (integridad referencial)");
            return;
        }

        if (!context.ProductoIds.Contains(idProducto!))
        {
            Reject(result, rowNumber, idOpinionTexto, $"IdProducto '{idProducto}' no existe en Productos (integridad referencial)");
            return;
        }

        if (!seenKeys.Add(idOpinionTexto!))
        {
            Reject(result, rowNumber, idOpinionTexto, "IdOpinion duplicado (ya existe en el archivo o en la base de datos)");
            return;
        }

        await InsertAsync(
            connection,
            @"INSERT INTO Encuestas (IdOpinion, IdCliente, IdProducto, Fecha, Comentario, Clasificacion, PuntajeSatisfaccion, Fuente)
              VALUES (@IdOpinion, @IdCliente, @IdProducto, @Fecha, @Comentario, @Clasificacion, @Puntaje, @Fuente)",
            p =>
            {
                p.AddWithValue("@IdOpinion", idOpinion);
                p.AddWithValue("@IdCliente", idCliente);
                p.AddWithValue("@IdProducto", idProducto);
                p.AddWithValue("@Fecha", fecha.Date);
                p.AddWithValue("@Comentario", comentario);
                p.AddWithValue("@Clasificacion", clasificacion);
                p.AddWithValue("@Puntaje", puntaje);
                p.AddWithValue("@Fuente", fuente);
            },
            rowNumber, idOpinionTexto, result, ct);
    }
}
