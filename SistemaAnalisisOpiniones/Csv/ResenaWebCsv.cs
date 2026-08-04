using CsvHelper.Configuration.Attributes;

namespace SistemaAnalisisOpiniones.Csv;

public class ResenaWebCsv
{
    [Name("IdReview")] public string? IdReview { get; set; }
    [Name("IdCliente")] public string? IdCliente { get; set; }
    [Name("IdProducto")] public string? IdProducto { get; set; }
    [Name("Fecha")] public string? Fecha { get; set; }
    [Name("Comentario")] public string? Comentario { get; set; }
    [Name("Rating")] public string? Rating { get; set; }
}
