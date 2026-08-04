using SistemaAnalisisOpiniones.Domain.Dtos;

namespace SistemaAnalisisOpiniones.Domain.Models;

/// <summary>
/// Contenedor con el resultado de la fase de extracción.
/// Cada extractor escribe únicamente sus propias colecciones, por lo que
/// pueden ejecutarse en paralelo sin necesidad de sincronización.
/// </summary>
public class DatosExtraidos
{
    // Fuente CSV (encuestas internas y catálogos)
    public List<ClienteDto> Clientes { get; set; } = new();
    public List<ProductoDto> Productos { get; set; } = new();
    public List<FuenteDatoDto> FuentesDato { get; set; } = new();
    public List<EncuestaDto> Encuestas { get; set; } = new();

    // Fuente base de datos relacional (reseñas del sitio web)
    public List<ResenaWebDto> Resenas { get; set; } = new();

    // Fuente API REST (comentarios de redes sociales)
    public List<ComentarioSocialDto> Comentarios { get; set; } = new();
}
