using SistemaAnalisisOpiniones.Domain.Models;

namespace SistemaAnalisisOpiniones.Application.Interfaces;

public interface IExtractor
{
    string NombreFuente { get; }

    bool Habilitado { get; }

    Task<ResultadoExtraccion> ExtraerAsync(DatosExtraidos destino, CancellationToken ct);
}
