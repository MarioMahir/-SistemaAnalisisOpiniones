namespace SistemaAnalisisOpiniones.Domain.Models;

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
