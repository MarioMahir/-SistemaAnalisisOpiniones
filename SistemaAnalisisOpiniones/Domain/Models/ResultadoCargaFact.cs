namespace SistemaAnalisisOpiniones.Domain.Models;

public class ResultadoCargaFact
{
    public string Fuente { get; init; } = "";
    public int Leidos { get; set; }
    public int Insertados { get; set; }
    public int Rechazados { get; set; }
    public long DuracionMs { get; set; }
    public bool Exitoso { get; set; }
    public string? Error { get; set; }
    public List<string> MotivosRechazo { get; } = new();
    public Dictionary<string, int> PorSentimiento { get; } = new();
}

public class InformeCargaFact
{
    public long FilasEliminadasLimpieza { get; set; }
    public long DuracionLimpiezaMs { get; set; }
    public List<ResultadoCargaFact> Cargas { get; } = new();
}
