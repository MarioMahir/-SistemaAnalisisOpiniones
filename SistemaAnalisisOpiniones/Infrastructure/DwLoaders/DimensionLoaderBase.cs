using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SistemaAnalisisOpiniones.Domain.Models;

namespace SistemaAnalisisOpiniones.Infrastructure.DwLoaders;

public abstract class DimensionLoaderBase
{
    protected readonly ILogger Logger;

    protected DimensionLoaderBase(ILogger logger) => Logger = logger;

    public abstract string NombreDimension { get; }

    public async Task<ResultadoCargaDimension> CargarAsync(
        SqlConnection staging, SqlConnection dw, CancellationToken ct)
    {
        var resultado = new ResultadoCargaDimension { Dimension = NombreDimension };
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Logger.LogInformation("Carga {Dimension}: iniciando...", NombreDimension);
            await EjecutarCargaAsync(staging, dw, resultado, ct);
            resultado.Exitoso = true;
        }
        catch (Exception ex)
        {
            resultado.Exitoso = false;
            resultado.Error = ex.Message;
            Logger.LogError(ex, "Carga {Dimension}: falló", NombreDimension);
        }
        finally
        {
            stopwatch.Stop();
            resultado.DuracionMs = stopwatch.ElapsedMilliseconds;
        }

        if (resultado.Exitoso)
        {
            Logger.LogInformation(
                "Carga {Dimension}: {Leidos} leídos, {Insertados} insertados, {Existentes} ya existían ({DuracionMs} ms)",
                NombreDimension, resultado.Leidos, resultado.Insertados, resultado.Existentes, resultado.DuracionMs);
        }

        return resultado;
    }

    protected abstract Task EjecutarCargaAsync(
        SqlConnection staging, SqlConnection dw, ResultadoCargaDimension resultado, CancellationToken ct);

    protected static async Task<HashSet<string>> CargarClavesExistentesAsync(
        SqlConnection dw, string sql, CancellationToken ct)
    {
        var claves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var command = new SqlCommand(sql, dw);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            if (!await reader.IsDBNullAsync(0, ct))
                claves.Add(Convert.ToString(reader.GetValue(0))!);
        }
        return claves;
    }
}
