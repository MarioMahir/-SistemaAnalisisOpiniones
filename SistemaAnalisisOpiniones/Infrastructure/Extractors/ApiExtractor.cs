using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaAnalisisOpiniones.Configuration;
using SistemaAnalisisOpiniones.Domain.Dtos;
using SistemaAnalisisOpiniones.Domain.Models;

namespace SistemaAnalisisOpiniones.Infrastructure.Extractors;

/// <summary>
/// Extrae los comentarios de redes sociales consumiendo la API REST
/// (TiendaSocialApi) mediante IHttpClientFactory.
/// </summary>
public class ApiExtractor : ExtractorBase
{
    public const string HttpClientName = "TiendaSocialApi";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApiFuenteOptions _options;

    public ApiExtractor(
        IHttpClientFactory httpClientFactory,
        IOptions<FuentesOptions> options,
        ILogger<ApiExtractor> logger)
        : base(logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value.Api;
    }

    public override string NombreFuente => "API REST (comentarios sociales)";
    public override bool Habilitado => _options.Enabled;

    protected override async Task<int> EjecutarExtraccionAsync(DatosExtraidos destino, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);

        var comentarios = await client.GetFromJsonAsync<List<ComentarioSocialDto>>(
            _options.EndpointComentarios, ct);

        destino.Comentarios = comentarios ?? new List<ComentarioSocialDto>();
        return destino.Comentarios.Count;
    }
}
