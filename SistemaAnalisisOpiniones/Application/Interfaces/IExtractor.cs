using SistemaAnalisisOpiniones.Domain.Models;

namespace SistemaAnalisisOpiniones.Application.Interfaces;

/// <summary>
/// Abstracción de una fuente de datos del proceso ETL.
/// Cada implementación (CSV, base de datos relacional, API REST) sabe
/// extraer sus registros y depositarlos en el contenedor compartido.
/// </summary>
public interface IExtractor
{
    /// <summary>Nombre descriptivo de la fuente, usado en logs y reportes.</summary>
    string NombreFuente { get; }

    /// <summary>Indica si la fuente está habilitada en la configuración.</summary>
    bool Habilitado { get; }

    /// <summary>
    /// Extrae los registros de la fuente y los escribe en <paramref name="destino"/>.
    /// No debe propagar excepciones: los errores se reportan en el resultado.
    /// </summary>
    Task<ResultadoExtraccion> ExtraerAsync(DatosExtraidos destino, CancellationToken ct);
}
