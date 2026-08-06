using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SistemaAnalisisOpiniones.Domain.Models;

namespace SistemaAnalisisOpiniones.Infrastructure.DwLoaders;

/// <summary>
/// Asegura el catálogo Dim_Sentimiento con las tres clasificaciones
/// válidas del modelo (coinciden con el CHECK de la tabla).
/// </summary>
public class DimSentimientoLoader : DimensionLoaderBase
{
    private static readonly string[] Catalogo = { "Positiva", "Negativa", "Neutra" };

    public DimSentimientoLoader(ILogger<DimSentimientoLoader> logger) : base(logger) { }

    public override string NombreDimension => "Dim_Sentimiento";

    protected override async Task EjecutarCargaAsync(
        SqlConnection staging, SqlConnection dw, ResultadoCargaDimension resultado, CancellationToken ct)
    {
        var existentes = await CargarClavesExistentesAsync(dw, "SELECT Clasificacion FROM Dim_Sentimiento", ct);

        foreach (var clasificacion in Catalogo)
        {
            resultado.Leidos++;
            if (existentes.Contains(clasificacion)) { resultado.Existentes++; continue; }

            await using var insert = new SqlCommand(
                "INSERT INTO Dim_Sentimiento (Clasificacion) VALUES (@Clasificacion)", dw);
            insert.Parameters.AddWithValue("@Clasificacion", clasificacion);
            await insert.ExecuteNonQueryAsync(ct);
            resultado.Insertados++;
        }
    }
}
