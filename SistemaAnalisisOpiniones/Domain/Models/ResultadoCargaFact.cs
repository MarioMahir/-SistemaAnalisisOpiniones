namespace SistemaAnalisisOpiniones.Domain.Models;

/// <summary>
/// Métricas de la carga de la tabla de hechos desde una fuente del staging:
/// cuántas opiniones se leyeron, cuántas se insertaron y cuántas se
/// rechazaron por no resolver alguna clave de dimensión.
/// </summary>
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
}

/// <summary>
/// Informe completo de la fase de carga de hechos: cuántas filas eliminó
/// la limpieza previa y el resultado de cada fuente del staging.
/// </summary>
public class InformeCargaFact
{
    public long FilasEliminadasLimpieza { get; set; }
    public long DuracionLimpiezaMs { get; set; }
    public List<ResultadoCargaFact> Cargas { get; } = new();
}
