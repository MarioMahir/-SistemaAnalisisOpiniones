using SistemaAnalisisOpiniones.Domain.Dtos;

namespace SistemaAnalisisOpiniones.Domain.Models;

public class DatosExtraidos
{
    public List<ClienteDto> Clientes { get; set; } = new();
    public List<ProductoDto> Productos { get; set; } = new();
    public List<FuenteDatoDto> FuentesDato { get; set; } = new();
    public List<EncuestaDto> Encuestas { get; set; } = new();

    public List<ResenaWebDto> Resenas { get; set; } = new();

    public List<ComentarioSocialDto> Comentarios { get; set; } = new();
}
