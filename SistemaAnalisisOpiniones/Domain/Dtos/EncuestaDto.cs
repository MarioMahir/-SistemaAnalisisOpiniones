using CsvHelper.Configuration.Attributes;

namespace SistemaAnalisisOpiniones.Domain.Dtos;

public class EncuestaDto
{
    [Name("IdOpinion")] public string? IdOpinion { get; set; }
    [Name("IdCliente")] public string? IdCliente { get; set; }
    [Name("IdProducto")] public string? IdProducto { get; set; }
    [Name("Fecha")] public string? Fecha { get; set; }
    [Name("Comentario")] public string? Comentario { get; set; }
    [Name("Clasificación")] public string? Clasificacion { get; set; }
    [Name("PuntajeSatisfacción")] public string? PuntajeSatisfaccion { get; set; }
    [Name("Fuente")] public string? Fuente { get; set; }
}
