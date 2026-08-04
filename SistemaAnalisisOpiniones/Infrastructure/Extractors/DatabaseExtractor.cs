using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaAnalisisOpiniones.Configuration;
using SistemaAnalisisOpiniones.Domain.Dtos;
using SistemaAnalisisOpiniones.Domain.Models;

namespace SistemaAnalisisOpiniones.Infrastructure.Extractors;

/// <summary>
/// Extrae las reseñas del sitio web desde la base de datos relacional de
/// origen (TiendaWebOrigen) ejecutando la consulta definida en configuración.
/// Los valores se entregan como texto crudo: la validación y conversión de
/// tipos ocurre después, en la fase de carga al staging.
/// </summary>
public class DatabaseExtractor : ExtractorBase
{
    private readonly BaseDatosFuenteOptions _options;

    public DatabaseExtractor(IOptions<FuentesOptions> options, ILogger<DatabaseExtractor> logger)
        : base(logger)
    {
        _options = options.Value.BaseDatos;
    }

    public override string NombreFuente => "Base de datos (reseñas web)";
    public override bool Habilitado => _options.Enabled;

    protected override async Task<int> EjecutarExtraccionAsync(DatosExtraidos destino, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand(_options.Query, connection);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var resenas = new List<ResenaWebDto>();
        while (await reader.ReadAsync(ct))
        {
            resenas.Add(new ResenaWebDto
            {
                IdReview = LeerTexto(reader, "IdReview"),
                IdCliente = LeerTexto(reader, "IdCliente"),
                IdProducto = LeerTexto(reader, "IdProducto"),
                Fecha = LeerTexto(reader, "Fecha"),
                Comentario = LeerTexto(reader, "Comentario"),
                Rating = LeerTexto(reader, "Rating"),
            });
        }

        destino.Resenas = resenas;
        return resenas.Count;
    }

    private static string? LeerTexto(SqlDataReader reader, string columna)
    {
        var ordinal = reader.GetOrdinal(columna);
        return reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal));
    }
}
