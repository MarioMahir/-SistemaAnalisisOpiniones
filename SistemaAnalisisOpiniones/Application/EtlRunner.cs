using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaAnalisisOpiniones.Application.Interfaces;
using SistemaAnalisisOpiniones.Configuration;
using SistemaAnalisisOpiniones.Domain.Models;
using SistemaAnalisisOpiniones.Infrastructure.Loaders;

namespace SistemaAnalisisOpiniones.Application;

public class EtlRunner
{
    private readonly EtlOptions _options;
    private readonly IReadOnlyList<IExtractor> _extractores;
    private readonly ClienteLoader _clienteLoader;
    private readonly ProductoLoader _productoLoader;
    private readonly FuenteDatoLoader _fuenteDatoLoader;
    private readonly EncuestaLoader _encuestaLoader;
    private readonly ResenaWebLoader _resenaWebLoader;
    private readonly ComentarioSocialLoader _comentarioSocialLoader;
    private readonly ILogger<EtlRunner> _logger;

    public EtlRunner(
        IOptions<EtlOptions> options,
        IEnumerable<IExtractor> extractores,
        ClienteLoader clienteLoader,
        ProductoLoader productoLoader,
        FuenteDatoLoader fuenteDatoLoader,
        EncuestaLoader encuestaLoader,
        ResenaWebLoader resenaWebLoader,
        ComentarioSocialLoader comentarioSocialLoader,
        ILogger<EtlRunner> logger)
    {
        _options = options.Value;
        _extractores = extractores.ToList();
        _clienteLoader = clienteLoader;
        _productoLoader = productoLoader;
        _fuenteDatoLoader = fuenteDatoLoader;
        _encuestaLoader = encuestaLoader;
        _resenaWebLoader = resenaWebLoader;
        _comentarioSocialLoader = comentarioSocialLoader;
        _logger = logger;
    }

    public async Task<InformeEtl> RunAsync(CancellationToken ct)
    {
        var total = Stopwatch.StartNew();

        var datos = new DatosExtraidos();
        var activos = _extractores.Where(e => e.Habilitado).ToList();

        foreach (var inactivo in _extractores.Except(activos))
            _logger.LogInformation("Fuente deshabilitada por configuración: {Fuente}", inactivo.NombreFuente);

        var faseExtraccion = Stopwatch.StartNew();
        var extracciones = await Task.WhenAll(activos.Select(e => e.ExtraerAsync(datos, ct)));
        faseExtraccion.Stop();

        _logger.LogInformation(
            "Fase de extracción completada: {Fuentes} fuentes en {DuracionMs} ms (en paralelo)",
            activos.Count, faseExtraccion.ElapsedMilliseconds);

        var faseCarga = Stopwatch.StartNew();
        var cargas = new List<EtlResult>();
        var context = new EtlContext();

        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(ct);

        cargas.Add(await _clienteLoader.RunAsync(datos.Clientes, connection, context, ct));
        cargas.Add(await _productoLoader.RunAsync(datos.Productos, connection, context, ct));
        cargas.Add(await _fuenteDatoLoader.RunAsync(datos.FuentesDato, connection, context, ct));

        cargas.Add(await _encuestaLoader.RunAsync(datos.Encuestas, connection, context, ct));
        cargas.Add(await _resenaWebLoader.RunAsync(datos.Resenas, connection, context, ct));
        cargas.Add(await _comentarioSocialLoader.RunAsync(datos.Comentarios, connection, context, ct));

        faseCarga.Stop();
        total.Stop();

        _logger.LogInformation(
            "Fase de carga completada en {CargaMs} ms. Proceso ETL total: {TotalMs} ms",
            faseCarga.ElapsedMilliseconds, total.ElapsedMilliseconds);

        return new InformeEtl(extracciones, cargas);
    }
}
