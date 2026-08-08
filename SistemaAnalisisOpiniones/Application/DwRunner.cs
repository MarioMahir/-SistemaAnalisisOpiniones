using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaAnalisisOpiniones.Configuration;
using SistemaAnalisisOpiniones.Domain.Models;
using SistemaAnalisisOpiniones.Infrastructure.DwLoaders;

namespace SistemaAnalisisOpiniones.Application;

public class DwRunner
{
    private readonly EtlOptions _etlOptions;
    private readonly DwOptions _dwOptions;
    private readonly IReadOnlyList<DimensionLoaderBase> _loaders;
    private readonly ILogger<DwRunner> _logger;

    public DwRunner(
        IOptions<EtlOptions> etlOptions,
        IOptions<DwOptions> dwOptions,
        DimClienteLoader dimCliente,
        DimProductoLoader dimProducto,
        DimFechaLoader dimFecha,
        DimFuenteLoader dimFuente,
        DimSentimientoLoader dimSentimiento,
        ILogger<DwRunner> logger)
    {
        _etlOptions = etlOptions.Value;
        _dwOptions = dwOptions.Value;
        _loaders = new DimensionLoaderBase[] { dimCliente, dimProducto, dimFecha, dimFuente, dimSentimiento };
        _logger = logger;
    }

    public async Task<IReadOnlyList<ResultadoCargaDimension>> RunAsync(CancellationToken ct)
    {
        var fase = Stopwatch.StartNew();
        var resultados = new List<ResultadoCargaDimension>();

        await using var staging = new SqlConnection(_etlOptions.ConnectionString);
        await using var dw = new SqlConnection(_dwOptions.ConnectionString);
        await staging.OpenAsync(ct);
        await dw.OpenAsync(ct);

        foreach (var loader in _loaders)
            resultados.Add(await loader.CargarAsync(staging, dw, ct));

        fase.Stop();
        _logger.LogInformation(
            "Fase de carga de dimensiones completada: {Dimensiones} dimensiones en {DuracionMs} ms",
            _loaders.Count, fase.ElapsedMilliseconds);

        return resultados;
    }
}
