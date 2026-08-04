using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SistemaAnalisisOpiniones.Domain.Models;

namespace SistemaAnalisisOpiniones.Infrastructure.Loaders;

/// <summary>
/// Plantilla de carga al staging: valida fila a fila los registros ya
/// extraídos, deduplica contra la base de datos y registra los rechazados.
/// </summary>
public abstract class StagingLoaderBase<TDto>
{
    protected readonly ILogger Logger;

    protected abstract string TableName { get; }

    protected StagingLoaderBase(ILogger logger) => Logger = logger;

    public async Task<EtlResult> RunAsync(
        IReadOnlyList<TDto> registros,
        SqlConnection connection,
        EtlContext context,
        CancellationToken ct)
    {
        var result = new EtlResult { TableName = TableName };

        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await PreloadAsync(connection, context, seenKeys, ct);

        var rowNumber = 1;
        foreach (var record in registros)
        {
            rowNumber++;
            result.Processed++;
            try
            {
                await ProcessRowAsync(record, rowNumber, connection, context, seenKeys, result, ct);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Excepción no controlada procesando la fila {Row} de {Table}", rowNumber, TableName);
                Reject(result, rowNumber, null, $"Excepción no controlada: {ex.Message}");
            }
        }

        return result;
    }

    protected virtual Task PreloadAsync(SqlConnection connection, EtlContext context, HashSet<string> seenKeys, CancellationToken ct)
        => Task.CompletedTask;

    protected abstract Task ProcessRowAsync(
        TDto record,
        int rowNumber,
        SqlConnection connection,
        EtlContext context,
        HashSet<string> seenKeys,
        EtlResult result,
        CancellationToken ct);

    protected static void Reject(EtlResult result, int rowNumber, string? key, string reason) =>
        result.RejectedRecords.Add(new RejectedRecord { RowNumber = rowNumber, Key = key, Reason = reason });

    protected async Task<bool> InsertAsync(
        SqlConnection connection,
        string sql,
        Action<SqlParameterCollection> addParameters,
        int rowNumber,
        string? key,
        EtlResult result,
        CancellationToken ct)
    {
        try
        {
            await using var command = new SqlCommand(sql, connection);
            addParameters(command.Parameters);
            await command.ExecuteNonQueryAsync(ct);
            result.Inserted++;
            return true;
        }
        catch (SqlException ex)
        {
            Logger.LogWarning(ex, "Fallo al insertar fila {Row} de {Table}", rowNumber, TableName);
            Reject(result, rowNumber, key, $"Error de base de datos: {ex.Message}");
            return false;
        }
    }

    protected static async Task<HashSet<string>> LoadExistingIdsAsync(SqlConnection connection, string sql, CancellationToken ct)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            if (!await reader.IsDBNullAsync(0, ct))
                ids.Add(reader.GetString(0));
        }
        return ids;
    }
}
