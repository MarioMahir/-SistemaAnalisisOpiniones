using CsvHelper.Configuration.Attributes;

namespace SistemaAnalisisOpiniones.Domain.Dtos;

public class ClienteDto
{
    [Name("IdCliente")] public string? IdCliente { get; set; }
    [Name("Nombre")] public string? Nombre { get; set; }
    [Name("Email")] public string? Email { get; set; }
}
