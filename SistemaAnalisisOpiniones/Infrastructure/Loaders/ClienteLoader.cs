using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SistemaAnalisisOpiniones.Domain.Dtos;
using SistemaAnalisisOpiniones.Domain.Models;

namespace SistemaAnalisisOpiniones.Infrastructure.Loaders;

public class ClienteLoader : StagingLoaderBase<ClienteDto>
{
    protected override string TableName => "Clientes";

    public ClienteLoader(ILogger<ClienteLoader> logger) : base(logger) { }

    protected override async Task PreloadAsync(SqlConnection connection, EtlContext context, HashSet<string> seenKeys, CancellationToken ct)
    {
        var existing = await LoadExistingIdsAsync(connection, "SELECT IdCliente FROM Clientes", ct);
        foreach (var id in existing)
        {
            context.ClienteIds.Add(id);
            seenKeys.Add(id);
        }
    }

    protected override async Task ProcessRowAsync(
        ClienteDto record, int rowNumber, SqlConnection connection, EtlContext context,
        HashSet<string> seenKeys, EtlResult result, CancellationToken ct)
    {
        var id = record.IdCliente?.Trim();
        var nombre = record.Nombre?.Trim();
        var email = record.Email?.Trim();

        if (!Validation.IsRequired(id) || !Validation.IsRequired(nombre) || !Validation.IsRequired(email))
        {
            Reject(result, rowNumber, id, "Campos obligatorios faltantes (IdCliente/Nombre/Email)");
            return;
        }

        if (!Validation.FitsLength(id, 20)) { Reject(result, rowNumber, id, "IdCliente excede 20 caracteres"); return; }
        if (!Validation.FitsLength(nombre, 100)) { Reject(result, rowNumber, id, "Nombre excede 100 caracteres"); return; }
        if (!Validation.FitsLength(email, 100)) { Reject(result, rowNumber, id, "Email excede 100 caracteres"); return; }
        if (!email!.Contains('@')) { Reject(result, rowNumber, id, "Formato de Email inválido"); return; }

        if (!seenKeys.Add(id!))
        {
            Reject(result, rowNumber, id, "IdCliente duplicado (ya existe en el archivo o en la base de datos)");
            return;
        }

        var inserted = await InsertAsync(
            connection,
            "INSERT INTO Clientes (IdCliente, Nombre, Email) VALUES (@IdCliente, @Nombre, @Email)",
            p =>
            {
                p.AddWithValue("@IdCliente", id);
                p.AddWithValue("@Nombre", nombre);
                p.AddWithValue("@Email", email);
            },
            rowNumber, id, result, ct);

        if (inserted) context.ClienteIds.Add(id!);
    }
}
