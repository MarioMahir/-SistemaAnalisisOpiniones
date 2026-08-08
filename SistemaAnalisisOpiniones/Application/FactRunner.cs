using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaAnalisisOpiniones.Configuration;
using SistemaAnalisisOpiniones.Domain.Models;
using SistemaAnalisisOpiniones.Infrastructure.DwLoaders;

namespace SistemaAnalisisOpiniones.Application;

/// <summary>
/// Orquesta la fase de carga de la tabla de hechos: primero el proceso de
/// limpieza (TRUNCATE de Fact_Opinion) y luego la carga desde las tres
/// fuentes del staging. Debe ejecutarse después de DwRunner, porque la
/// tabla de hechos depende de que las cinco dimensiones estén pobladas.
/// </summary>
public class FactRunner
{
    private readonly EtlOptions _etlOptions;
    private readonly DwOptions _dwOptions;
    private readonly FactOpinionLoader _loader;
    private readonly ILogger<FactRunner> _logger;

    public FactRunner(
        IOptions<EtlOptions> etlOptions,
        IOptions<DwOptions> dwOptions,
        FactOpinionLoader loader,
        ILogger<FactRunner> logger)
    {
        _etlOptions = etlOptions.Value;
        _dwOptions = dwOptions.Value;
        _loader = loader;
        _logger = logger;
    }

    public async Task<InformeCargaFact> RunAsync(CancellationToken ct)
    {
        var fase = Stopwatch.StartNew();
        var informe = new InformeCargaFact();

        await using var staging = new SqlConnection(_etlOptions.ConnectionString);
        await using var dw = new SqlConnection(_dwOptions.ConnectionString);
        await staging.OpenAsync(ct);
        await dw.OpenAsync(ct);

        (informe.FilasEliminadasLimpieza, informe.DuracionLimpiezaMs) = await _loader.LimpiarAsync(dw, ct);

        await _loader.PrecargarDimensionesAsync(dw, ct);

        informe.Cargas.Add(await _loader.CargarEncuestasAsync(staging, dw, ct));
        informe.Cargas.Add(await _loader.CargarResenasWebAsync(staging, dw, ct));
        informe.Cargas.Add(await _loader.CargarComentariosSocialesAsync(staging, dw, ct));

        fase.Stop();
        _logger.LogInformation(
            "Fase de carga de hechos completada: {Insertados} opiniones insertadas en Fact_Opinion en {DuracionMs} ms",
            informe.Cargas.Sum(c => c.Insertados), fase.ElapsedMilliseconds);

        return informe;
    }
}
