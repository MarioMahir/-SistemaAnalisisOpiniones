using CsvHelper.Configuration.Attributes;

namespace SistemaAnalisisOpiniones.Domain.Dtos;

public class ProductoDto
{
    [Name("ProductID")] public string? ProductID { get; set; }
    [Name("ProductName")] public string? ProductName { get; set; }
    [Name("Category")] public string? Category { get; set; }
    [Name("Price")] public string? Price { get; set; }
    [Name("Stock")] public string? Stock { get; set; }
}
