using CsvHelper.Configuration.Attributes;

namespace TiendaSocialApi;

/// <summary>
/// Comentario publicado en redes sociales sobre un producto.
/// Los campos se exponen tal cual llegan de la plataforma social,
/// sin validar: la limpieza corresponde al consumidor (proceso ETL).
/// </summary>
public class ComentarioSocial
{
    [Name("IdComment")] public string? IdComment { get; set; }
    [Name("IdCliente")] public string? IdCliente { get; set; }
    [Name("IdProducto")] public string? IdProducto { get; set; }
    [Name("Fuente")] public string? Fuente { get; set; }
    [Name("Fecha")] public string? Fecha { get; set; }
    [Name("Comentario")] public string? Comentario { get; set; }
}
