using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using TiendaSocialApi;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Los comentarios se cargan una sola vez al arrancar y se sirven desde memoria.
var comentarios = CargarComentarios(app.Environment.ContentRootPath, app.Logger);

app.MapGet("/api/comments", (string? fuente) =>
{
    var resultado = string.IsNullOrWhiteSpace(fuente)
        ? comentarios
        : comentarios.Where(c => string.Equals(c.Fuente, fuente, StringComparison.OrdinalIgnoreCase)).ToList();

    return Results.Ok(resultado);
});

app.MapGet("/api/comments/{id}", (string id) =>
{
    var comentario = comentarios.FirstOrDefault(c => string.Equals(c.IdComment, id, StringComparison.OrdinalIgnoreCase));
    return comentario is null ? Results.NotFound() : Results.Ok(comentario);
});

app.Run();

static List<ComentarioSocial> CargarComentarios(string contentRootPath, ILogger logger)
{
    var ruta = Path.Combine(contentRootPath, "Data", "social_comments.csv");
    if (!File.Exists(ruta))
    {
        logger.LogWarning("No se encontró {Ruta}; la API arrancará sin comentarios.", ruta);
        return new List<ComentarioSocial>();
    }

    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        HeaderValidated = null,
        MissingFieldFound = null,
        TrimOptions = TrimOptions.Trim,
    };

    using var reader = new StreamReader(ruta, detectEncodingFromByteOrderMarks: true);
    using var csv = new CsvReader(reader, config);
    var lista = csv.GetRecords<ComentarioSocial>().ToList();
    logger.LogInformation("Cargados {Cantidad} comentarios sociales desde {Ruta}.", lista.Count, ruta);
    return lista;
}
