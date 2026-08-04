using CsvHelper.Configuration.Attributes;

namespace SistemaAnalisisOpiniones.Domain.Dtos;

public class FuenteDatoDto
{
    [Name("IdFuente")] public string? IdFuente { get; set; }
    [Name("TipoFuente")] public string? TipoFuente { get; set; }
    [Name("FechaCarga")] public string? FechaCarga { get; set; }
}
