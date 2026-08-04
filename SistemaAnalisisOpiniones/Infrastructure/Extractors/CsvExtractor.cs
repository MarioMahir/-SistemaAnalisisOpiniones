using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaAnalisisOpiniones.Configuration;
using SistemaAnalisisOpiniones.Domain.Dtos;
using SistemaAnalisisOpiniones.Domain.Models;

namespace SistemaAnalisisOpiniones.Infrastructure.Extractors;

/// <summary>
/// Extrae las encuestas internas y los catálogos (clientes, productos,
/// fuentes de dato) desde archivos CSV usando CsvHelper.
/// </summary>
public class CsvExtractor : ExtractorBase
{
    private readonly CsvFuenteOptions _options;

    public CsvExtractor(IOptions<FuentesOptions> options, ILogger<CsvExtractor> logger)
        : base(logger)
    {
        _options = options.Value.Csv;
    }

    public override string NombreFuente => "CSV (encuestas y catálogos)";
    public override bool Habilitado => _options.Enabled;

    protected override Task<int> EjecutarExtraccionAsync(DatosExtraidos destino, CancellationToken ct)
    {
        destino.Clientes = Leer<ClienteDto>(_options.ArchivoClientes);
        destino.Productos = Leer<ProductoDto>(_options.ArchivoProductos);
        destino.FuentesDato = Leer<FuenteDatoDto>(_options.ArchivoFuentesDato);
        destino.Encuestas = Leer<EncuestaDto>(_options.ArchivoEncuestas);

        var total = destino.Clientes.Count + destino.Productos.Count +
                    destino.FuentesDato.Count + destino.Encuestas.Count;
        return Task.FromResult(total);
    }

    private List<T> Leer<T>(string archivo)
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, _options.Carpeta, archivo);
        var registros = CsvFileReader.Read<T>(ruta);
        Logger.LogInformation("CSV {Archivo}: {Cantidad} registros leídos", archivo, registros.Count);
        return registros;
    }
}
