using CsvHelper.Configuration.Attributes;

namespace SistemaAnalisisOpiniones.Csv;

public class ComentarioSocialCsv
{
    [Name("IdComment")] public string? IdComment { get; set; }
    [Name("IdCliente")] public string? IdCliente { get; set; }
    [Name("IdProducto")] public string? IdProducto { get; set; }
    [Name("Fuente")] public string? Fuente { get; set; }
    [Name("Fecha")] public string? Fecha { get; set; }
    [Name("Comentario")] public string? Comentario { get; set; }
}
