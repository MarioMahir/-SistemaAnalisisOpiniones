using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SistemaAnalisisOpiniones.Domain.Dtos;
using SistemaAnalisisOpiniones.Domain.Models;

namespace SistemaAnalisisOpiniones.Infrastructure.Loaders;

public class ProductoLoader : StagingLoaderBase<ProductoDto>
{
    protected override string TableName => "Productos";

    public ProductoLoader(ILogger<ProductoLoader> logger) : base(logger) { }

    protected override async Task PreloadAsync(SqlConnection connection, EtlContext context, HashSet<string> seenKeys, CancellationToken ct)
    {
        var existing = await LoadExistingIdsAsync(connection, "SELECT IdProducto FROM Productos", ct);
        foreach (var id in existing)
        {
            context.ProductoIds.Add(id);
            seenKeys.Add(id);
        }
    }

    protected override async Task ProcessRowAsync(
        ProductoDto record, int rowNumber, SqlConnection connection, EtlContext context,
        HashSet<string> seenKeys, EtlResult result, CancellationToken ct)
    {
        var id = record.ProductID?.Trim();
        var nombre = record.ProductName?.Trim();
        var categoria = record.Category?.Trim();

        if (!Validation.IsRequired(id) || !Validation.IsRequired(nombre) || !Validation.IsRequired(categoria))
        {
            Reject(result, rowNumber, id, "Campos obligatorios faltantes (IdProducto/Nombre/Categoria)");
            return;
        }

        if (!Validation.FitsLength(id, 20)) { Reject(result, rowNumber, id, "IdProducto excede 20 caracteres"); return; }
        if (!Validation.FitsLength(nombre, 100)) { Reject(result, rowNumber, id, "Nombre excede 100 caracteres"); return; }
        if (!Validation.FitsLength(categoria, 50)) { Reject(result, rowNumber, id, "Categoria excede 50 caracteres"); return; }

        if (!seenKeys.Add(id!))
        {
            Reject(result, rowNumber, id, "IdProducto duplicado (ya existe en el archivo o en la base de datos)");
            return;
        }

        var inserted = await InsertAsync(
            connection,
            "INSERT INTO Productos (IdProducto, Nombre, Categoria) VALUES (@IdProducto, @Nombre, @Categoria)",
            p =>
            {
                p.AddWithValue("@IdProducto", id);
                p.AddWithValue("@Nombre", nombre);
                p.AddWithValue("@Categoria", categoria);
            },
            rowNumber, id, result, ct);

        if (inserted) context.ProductoIds.Add(id!);
    }
}
