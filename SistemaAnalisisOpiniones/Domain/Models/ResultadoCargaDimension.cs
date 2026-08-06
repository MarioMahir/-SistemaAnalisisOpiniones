namespace SistemaAnalisisOpiniones.Domain.Models;

/// <summary>
/// Métricas de la carga de una dimensión del Data Warehouse:
/// cuántos registros se leyeron del staging, cuántos se insertaron
/// y cuántos ya existían (la carga es incremental e idempotente).
/// </summary>
public class ResultadoCargaDimension
{
    public string Dimension { get; init; } = "";
    public int Leidos { get; set; }
    public int Insertados { get; set; }
    public int Existentes { get; set; }
    public long DuracionMs { get; set; }
    public bool Exitoso { get; set; }
    public string? Error { get; set; }
}
