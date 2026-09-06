using System.Globalization;
using System.Text.Json;
using SistemaAnalisisOpiniones.Dashboard;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<DwRepository>();
// Las filas se devuelven como diccionarios: sus claves también van en camelCase para el JavaScript.
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase);

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Indicadores generales y series para las gráficas del dashboard.
app.MapGet("/api/resumen", async (DwRepository dw, CancellationToken ct) =>
    Results.Ok((await dw.ResumenAsync(ct)).FirstOrDefault()));

app.MapGet("/api/sentimientos", async (DwRepository dw, CancellationToken ct) =>
    Results.Ok(await dw.SentimientosAsync(ct)));

app.MapGet("/api/fuentes", async (DwRepository dw, CancellationToken ct) =>
    Results.Ok(await dw.FuentesAsync(ct)));

app.MapGet("/api/tendencia", async (DwRepository dw, CancellationToken ct) =>
    Results.Ok(await dw.TendenciaMensualAsync(ct)));

app.MapGet("/api/productos", async (DwRepository dw, int top, CancellationToken ct) =>
    Results.Ok(await dw.ProductosAsync(Math.Clamp(top <= 0 ? 10 : top, 1, 50), ct)));

app.MapGet("/api/productos/lista", async (DwRepository dw, CancellationToken ct) =>
    Results.Ok(await dw.ListaProductosAsync(ct)));

// Detalle de un producto en un rango de fechas: tendencia mensual y opiniones.
app.MapGet("/api/productos/{idProducto}/tendencia", async (DwRepository dw, string idProducto, string? desde, string? hasta, CancellationToken ct) =>
{
    if (!TryRango(desde, hasta, out var d, out var h))
        return Results.BadRequest("Fechas inválidas: use el formato yyyy-MM-dd.");
    return Results.Ok(await dw.TendenciaProductoAsync(idProducto, d, h, ct));
});

app.MapGet("/api/productos/{idProducto}/opiniones", async (DwRepository dw, string idProducto, string? desde, string? hasta, CancellationToken ct) =>
{
    if (!TryRango(desde, hasta, out var d, out var h))
        return Results.BadRequest("Fechas inválidas: use el formato yyyy-MM-dd.");
    return Results.Ok(await dw.OpinionesAsync(idProducto, d, h, ct));
});

app.Run();

static bool TryRango(string? desde, string? hasta, out DateTime d, out DateTime h)
{
    d = new DateTime(2000, 1, 1);
    h = DateTime.Today.AddYears(1);
    var okDesde = string.IsNullOrWhiteSpace(desde) || DateTime.TryParseExact(desde, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d);
    var okHasta = string.IsNullOrWhiteSpace(hasta) || DateTime.TryParseExact(hasta, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out h);
    return okDesde && okHasta && d <= h;
}
