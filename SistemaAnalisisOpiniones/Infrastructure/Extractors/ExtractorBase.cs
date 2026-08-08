using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SistemaAnalisisOpiniones.Application.Interfaces;
using SistemaAnalisisOpiniones.Domain.Models;

namespace SistemaAnalisisOpiniones.Infrastructure.Extractors;

public abstract class ExtractorBase : IExtractor
{
    protected readonly ILogger Logger;

    protected ExtractorBase(ILogger logger) => Logger = logger;

    public abstract string NombreFuente { get; }
    public abstract bool Habilitado { get; }

    public async Task<ResultadoExtraccion> ExtraerAsync(DatosExtraidos destino, CancellationToken ct)
    {
        var resultado = new ResultadoExtraccion { Fuente = NombreFuente };
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Logger.LogInformation("Extracción {Fuente}: iniciando...", NombreFuente);
            resultado.RegistrosExtraidos = await EjecutarExtraccionAsync(destino, ct);
            resultado.Exitoso = true;
        }
        catch (Exception ex)
        {
            resultado.Exitoso = false;
            resultado.Error = ex.Message;
            Logger.LogError(ex, "Extracción {Fuente}: falló", NombreFuente);
        }
        finally
        {
            stopwatch.Stop();
            resultado.DuracionMs = stopwatch.ElapsedMilliseconds;
        }

        if (resultado.Exitoso)
        {
            Logger.LogInformation(
                "Extracción {Fuente}: {Registros} registros en {DuracionMs} ms",
                NombreFuente, resultado.RegistrosExtraidos, resultado.DuracionMs);
        }

        return resultado;
    }

    protected abstract Task<int> EjecutarExtraccionAsync(DatosExtraidos destino, CancellationToken ct);
}
