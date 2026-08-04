namespace SistemaAnalisisOpiniones.Domain.Models;

/// <summary>
/// Resultado completo de una corrida del proceso ETL:
/// métricas de la fase de extracción y de la fase de carga al staging.
/// </summary>
public record InformeEtl(
    IReadOnlyList<ResultadoExtraccion> Extracciones,
    IReadOnlyList<EtlResult> Cargas);
