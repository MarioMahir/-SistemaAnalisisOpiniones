using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SistemaAnalisisOpiniones.Domain.Dtos;
using SistemaAnalisisOpiniones.Domain.Models;

namespace SistemaAnalisisOpiniones.Infrastructure.Loaders;

public class FuenteDatoLoader : StagingLoaderBase<FuenteDatoDto>
{
    protected override string TableName => "FuenteDatos";

    public FuenteDatoLoader(ILogger<FuenteDatoLoader> logger) : base(logger) { }

    protected override async Task PreloadAsync(SqlConnection connection, EtlContext context, HashSet<string> seenKeys, CancellationToken ct)
    {
        var existing = await LoadExistingIdsAsync(connection, "SELECT IdFuente FROM FuenteDatos", ct);
        foreach (var id in existing) seenKeys.Add(id);
    }

    protected override async Task ProcessRowAsync(
        FuenteDatoDto record, int rowNumber, SqlConnection connection, EtlContext context,
        HashSet<string> seenKeys, EtlResult result, CancellationToken ct)
    {
        var id = record.IdFuente?.Trim();
        var tipo = record.TipoFuente?.Trim();
        var fechaTexto = record.FechaCarga?.Trim();

        if (!Validation.IsRequired(id) || !Validation.IsRequired(tipo) || !Validation.IsRequired(fechaTexto))
        {
            Reject(result, rowNumber, id, "Campos obligatorios faltantes (IdFuente/TipoFuente/FechaCarga)");
            return;
        }

        if (!Validation.FitsLength(id, 20)) { Reject(result, rowNumber, id, "IdFuente excede 20 caracteres"); return; }
        if (!Validation.FitsLength(tipo, 50)) { Reject(result, rowNumber, id, "TipoFuente excede 50 caracteres"); return; }

        if (!Validation.TryParseDate(fechaTexto, out var fecha))
        {
            Reject(result, rowNumber, id, $"Formato de fecha inválido: '{fechaTexto}'");
            return;
        }

        if (!seenKeys.Add(id!))
        {
            Reject(result, rowNumber, id, "IdFuente duplicado (ya existe en el archivo o en la base de datos)");
            return;
        }

        await InsertAsync(
            connection,
            "INSERT INTO FuenteDatos (IdFuente, TipoFuente, FechaCarga) VALUES (@IdFuente, @TipoFuente, @FechaCarga)",
            p =>
            {
                p.AddWithValue("@IdFuente", id);
                p.AddWithValue("@TipoFuente", tipo);
                p.AddWithValue("@FechaCarga", fecha.Date);
            },
            rowNumber, id, result, ct);
    }
}
