namespace SistemaAnalisisOpiniones.Domain.Models;

public record InformeEtl(
    IReadOnlyList<ResultadoExtraccion> Extracciones,
    IReadOnlyList<EtlResult> Cargas);
