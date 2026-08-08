using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SistemaAnalisisOpiniones.Domain.Models;

namespace SistemaAnalisisOpiniones.Infrastructure.DwLoaders;

public class DimFuenteLoader : DimensionLoaderBase
{
    private static readonly (string Tipo, string Descripcion)[] Catalogo =
    {
        ("Encuesta",  "Encuestas internas de satisfacción (CSV)"),
        ("Web",       "Reseñas publicadas en el sitio web (BD relacional)"),
        ("RedSocial", "Comentarios de redes sociales (API REST)"),
    };

    public DimFuenteLoader(ILogger<DimFuenteLoader> logger) : base(logger) { }

    public override string NombreDimension => "Dim_Fuente";

    protected override async Task EjecutarCargaAsync(
        SqlConnection staging, SqlConnection dw, ResultadoCargaDimension resultado, CancellationToken ct)
    {
        var existentes = await CargarClavesExistentesAsync(dw, "SELECT TipoFuente FROM Dim_Fuente", ct);

        foreach (var (tipo, descripcion) in Catalogo)
        {
            resultado.Leidos++;
            if (existentes.Contains(tipo)) { resultado.Existentes++; continue; }

            await using var insert = new SqlCommand(
                "INSERT INTO Dim_Fuente (TipoFuente, Descripcion) VALUES (@TipoFuente, @Descripcion)", dw);
            insert.Parameters.AddWithValue("@TipoFuente", tipo);
            insert.Parameters.AddWithValue("@Descripcion", descripcion);
            await insert.ExecuteNonQueryAsync(ct);
            resultado.Insertados++;
        }
    }
}
