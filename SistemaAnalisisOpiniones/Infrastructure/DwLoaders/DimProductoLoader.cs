using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SistemaAnalisisOpiniones.Domain.Models;

namespace SistemaAnalisisOpiniones.Infrastructure.DwLoaders;

/// <summary>
/// Carga Dim_Producto desde la tabla Productos del staging.
/// La clave de negocio es IdProductoOrigen; la dimensión se mantiene
/// deliberadamente desnormalizada (Categoria como columna plana).
/// </summary>
public class DimProductoLoader : DimensionLoaderBase
{
    public DimProductoLoader(ILogger<DimProductoLoader> logger) : base(logger) { }

    public override string NombreDimension => "Dim_Producto";

    protected override async Task EjecutarCargaAsync(
        SqlConnection staging, SqlConnection dw, ResultadoCargaDimension resultado, CancellationToken ct)
    {
        var existentes = await CargarClavesExistentesAsync(dw, "SELECT IdProductoOrigen FROM Dim_Producto", ct);

        await using var lectura = new SqlCommand("SELECT IdProducto, Nombre, Categoria FROM Productos", staging);
        await using var reader = await lectura.ExecuteReaderAsync(ct);

        var nuevos = new List<(string Id, string Nombre, string? Categoria)>();
        while (await reader.ReadAsync(ct))
        {
            resultado.Leidos++;
            var id = reader.GetString(0);
            if (existentes.Contains(id)) { resultado.Existentes++; continue; }
            nuevos.Add((id, reader.GetString(1), await reader.IsDBNullAsync(2, ct) ? null : reader.GetString(2)));
        }

        foreach (var p in nuevos)
        {
            await using var insert = new SqlCommand(
                @"INSERT INTO Dim_Producto (IdProductoOrigen, Nombre, Categoria)
                  VALUES (@IdProductoOrigen, @Nombre, @Categoria)", dw);
            insert.Parameters.AddWithValue("@IdProductoOrigen", p.Id);
            insert.Parameters.AddWithValue("@Nombre", p.Nombre);
            insert.Parameters.AddWithValue("@Categoria", (object?)p.Categoria ?? DBNull.Value);
            await insert.ExecuteNonQueryAsync(ct);
            resultado.Insertados++;
        }
    }
}
