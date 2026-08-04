using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SistemaAnalisisOpiniones.Infrastructure;
using SistemaAnalisisOpiniones.Models;

namespace SistemaAnalisisOpiniones.Etl;

public class EtlRunner
{
    private readonly EtlOptions _options;
    private readonly ClienteLoader _clienteLoader;
    private readonly ProductoLoader _productoLoader;
    private readonly FuenteDatoLoader _fuenteDatoLoader;
    private readonly EncuestaLoader _encuestaLoader;
    private readonly ResenaWebLoader _resenaWebLoader;
    private readonly ComentarioSocialLoader _comentarioSocialLoader;

    public EtlRunner(
        IOptions<EtlOptions> options,
        ClienteLoader clienteLoader,
        ProductoLoader productoLoader,
        FuenteDatoLoader fuenteDatoLoader,
        EncuestaLoader encuestaLoader,
        ResenaWebLoader resenaWebLoader,
        ComentarioSocialLoader comentarioSocialLoader)
    {
        _options = options.Value;
        _clienteLoader = clienteLoader;
        _productoLoader = productoLoader;
        _fuenteDatoLoader = fuenteDatoLoader;
        _encuestaLoader = encuestaLoader;
        _resenaWebLoader = resenaWebLoader;
        _comentarioSocialLoader = comentarioSocialLoader;
    }

    public async Task<List<EtlResult>> RunAsync(CancellationToken ct)
    {
        var results = new List<EtlResult>();
        var context = new EtlContext();

        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(ct);

        results.Add(await _clienteLoader.RunAsync(connection, context, ct));
        results.Add(await _productoLoader.RunAsync(connection, context, ct));
        results.Add(await _fuenteDatoLoader.RunAsync(connection, context, ct));

        results.Add(await _encuestaLoader.RunAsync(connection, context, ct));
        results.Add(await _resenaWebLoader.RunAsync(connection, context, ct));
        results.Add(await _comentarioSocialLoader.RunAsync(connection, context, ct));

        return results;
    }
}
