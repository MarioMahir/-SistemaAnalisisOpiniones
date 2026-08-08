namespace SistemaAnalisisOpiniones.Domain.Models;

public class ResultadoExtraccion
{
    public string Fuente { get; init; } = "";
    public int RegistrosExtraidos { get; set; }
    public long DuracionMs { get; set; }
    public bool Exitoso { get; set; }
    public string? Error { get; set; }
}
