namespace SistemaAnalisisOpiniones.Domain.Models;

/// <summary>
/// Métricas de la corrida de un extractor: cuántos registros entregó,
/// cuánto tardó y si terminó con éxito.
/// </summary>
public class ResultadoExtraccion
{
    public string Fuente { get; init; } = "";
    public int RegistrosExtraidos { get; set; }
    public long DuracionMs { get; set; }
    public bool Exitoso { get; set; }
    public string? Error { get; set; }
}
