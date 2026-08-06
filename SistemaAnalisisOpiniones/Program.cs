using SistemaAnalisisOpiniones;
using SistemaAnalisisOpiniones.Application;
using SistemaAnalisisOpiniones.Application.Interfaces;
using SistemaAnalisisOpiniones.Configuration;
using SistemaAnalisisOpiniones.Infrastructure.DwLoaders;
using SistemaAnalisisOpiniones.Infrastructure.Extractors;
using SistemaAnalisisOpiniones.Infrastructure.Loaders;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<EtlOptions>(builder.Configuration.GetSection("Etl"));
builder.Services.Configure<FuentesOptions>(builder.Configuration.GetSection("Fuentes"));
builder.Services.Configure<DwOptions>(builder.Configuration.GetSection("Dw"));

// Cliente HTTP para la API de comentarios sociales.
var apiOptions = builder.Configuration.GetSection("Fuentes:Api").Get<ApiFuenteOptions>() ?? new ApiFuenteOptions();
builder.Services.AddHttpClient(ApiExtractor.HttpClientName, client =>
{
    client.BaseAddress = new Uri(apiOptions.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(apiOptions.TimeoutSegundos);
});

// Extractores: una implementación de IExtractor por cada fuente de datos.
builder.Services.AddSingleton<IExtractor, CsvExtractor>();
builder.Services.AddSingleton<IExtractor, DatabaseExtractor>();
builder.Services.AddSingleton<IExtractor, ApiExtractor>();

// Cargadores del staging.
builder.Services.AddSingleton<ClienteLoader>();
builder.Services.AddSingleton<ProductoLoader>();
builder.Services.AddSingleton<FuenteDatoLoader>();
builder.Services.AddSingleton<EncuestaLoader>();
builder.Services.AddSingleton<ResenaWebLoader>();
builder.Services.AddSingleton<ComentarioSocialLoader>();

// Cargadores de las dimensiones del Data Warehouse.
builder.Services.AddSingleton<DimClienteLoader>();
builder.Services.AddSingleton<DimProductoLoader>();
builder.Services.AddSingleton<DimFechaLoader>();
builder.Services.AddSingleton<DimFuenteLoader>();
builder.Services.AddSingleton<DimSentimientoLoader>();

builder.Services.AddSingleton<EtlRunner>();
builder.Services.AddSingleton<DwRunner>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
