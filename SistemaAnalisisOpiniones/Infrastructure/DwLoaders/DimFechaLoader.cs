using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SistemaAnalisisOpiniones.Domain.Models;

namespace SistemaAnalisisOpiniones.Infrastructure.DwLoaders;

/// <summary>
/// Carga Dim_Fecha con las fechas distintas en que ocurrieron opiniones
/// (encuestas, reseñas web y comentarios sociales del staging).
/// Los atributos del calendario (año, trimestre, mes, día de la semana)
/// se calculan en la fase de transformación, en español.
/// La clave IdFechaDim es una clave inteligente con formato AAAAMMDD.
/// </summary>
public class DimFechaLoader : DimensionLoaderBase
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-ES");

    public DimFechaLoader(ILogger<DimFechaLoader> logger) : base(logger) { }

    public override string NombreDimension => "Dim_Fecha";

    protected override async Task EjecutarCargaAsync(
        SqlConnection staging, SqlConnection dw, ResultadoCargaDimension resultado, CancellationToken ct)
    {
        var existentes = await CargarClavesExistentesAsync(dw, "SELECT IdFechaDim FROM Dim_Fecha", ct);

        const string sqlFechas = @"
            SELECT Fecha FROM Encuestas
            UNION
            SELECT Fecha FROM ResenasWeb
            UNION
            SELECT Fecha FROM ComentariosSociales";

        var fechas = new List<DateTime>();
        await using (var lectura = new SqlCommand(sqlFechas, staging))
        await using (var reader = await lectura.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                if (!await reader.IsDBNullAsync(0, ct))
                    fechas.Add(reader.GetDateTime(0).Date);
            }
        }

        foreach (var fecha in fechas.OrderBy(f => f))
        {
            resultado.Leidos++;
            var idFecha = fecha.Year * 10000 + fecha.Month * 100 + fecha.Day;
            if (existentes.Contains(idFecha.ToString())) { resultado.Existentes++; continue; }

            await using var insert = new SqlCommand(
                @"INSERT INTO Dim_Fecha (IdFechaDim, Fecha, Anio, Trimestre, Mes, NombreMes, Dia, DiaSemana)
                  VALUES (@IdFechaDim, @Fecha, @Anio, @Trimestre, @Mes, @NombreMes, @Dia, @DiaSemana)", dw);
            insert.Parameters.AddWithValue("@IdFechaDim", idFecha);
            insert.Parameters.AddWithValue("@Fecha", fecha);
            insert.Parameters.AddWithValue("@Anio", fecha.Year);
            insert.Parameters.AddWithValue("@Trimestre", (byte)((fecha.Month + 2) / 3));
            insert.Parameters.AddWithValue("@Mes", (byte)fecha.Month);
            insert.Parameters.AddWithValue("@NombreMes", Capitalizar(fecha.ToString("MMMM", Cultura)));
            insert.Parameters.AddWithValue("@Dia", (byte)fecha.Day);
            insert.Parameters.AddWithValue("@DiaSemana", Capitalizar(fecha.ToString("dddd", Cultura)));
            await insert.ExecuteNonQueryAsync(ct);
            resultado.Insertados++;
        }
    }

    private static string Capitalizar(string texto) =>
        texto.Length == 0 ? texto : char.ToUpper(texto[0], Cultura) + texto[1..];
}
