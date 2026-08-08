using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SistemaAnalisisOpiniones.Domain.Models;

namespace SistemaAnalisisOpiniones.Infrastructure.DwLoaders;

public class DimClienteLoader : DimensionLoaderBase
{
    public DimClienteLoader(ILogger<DimClienteLoader> logger) : base(logger) { }

    public override string NombreDimension => "Dim_Cliente";

    protected override async Task EjecutarCargaAsync(
        SqlConnection staging, SqlConnection dw, ResultadoCargaDimension resultado, CancellationToken ct)
    {
        var existentes = await CargarClavesExistentesAsync(dw, "SELECT IdClienteOrigen FROM Dim_Cliente", ct);

        await using var lectura = new SqlCommand("SELECT IdCliente, Nombre, Email FROM Clientes", staging);
        await using var reader = await lectura.ExecuteReaderAsync(ct);

        var nuevos = new List<(string Id, string Nombre, string? Email)>();
        while (await reader.ReadAsync(ct))
        {
            resultado.Leidos++;
            var id = reader.GetString(0);
            if (existentes.Contains(id)) { resultado.Existentes++; continue; }
            nuevos.Add((id, reader.GetString(1), await reader.IsDBNullAsync(2, ct) ? null : reader.GetString(2)));
        }

        foreach (var c in nuevos)
        {
            await using var insert = new SqlCommand(
                @"INSERT INTO Dim_Cliente (IdClienteOrigen, Nombre, Email)
                  VALUES (@IdClienteOrigen, @Nombre, @Email)", dw);
            insert.Parameters.AddWithValue("@IdClienteOrigen", c.Id);
            insert.Parameters.AddWithValue("@Nombre", c.Nombre);
            insert.Parameters.AddWithValue("@Email", (object?)c.Email ?? DBNull.Value);
            await insert.ExecuteNonQueryAsync(ct);
            resultado.Insertados++;
        }
    }
}
