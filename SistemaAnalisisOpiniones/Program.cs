using SistemaAnalisisOpiniones;
using SistemaAnalisisOpiniones.Etl;
using SistemaAnalisisOpiniones.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<EtlOptions>(builder.Configuration.GetSection("Etl"));

builder.Services.AddSingleton<ClienteLoader>();
builder.Services.AddSingleton<ProductoLoader>();
builder.Services.AddSingleton<FuenteDatoLoader>();
builder.Services.AddSingleton<EncuestaLoader>();
builder.Services.AddSingleton<ResenaWebLoader>();
builder.Services.AddSingleton<ComentarioSocialLoader>();
builder.Services.AddSingleton<EtlRunner>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
