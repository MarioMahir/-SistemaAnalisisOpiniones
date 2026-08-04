using SistemaAnalisisOpiniones.Application;

namespace SistemaAnalisisOpiniones;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly EtlRunner _runner;
    private readonly IHostApplicationLifetime _lifetime;

    public Worker(ILogger<Worker> logger, EtlRunner runner, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _runner = runner;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Iniciando proceso ETL: Sistema de Análisis de Opiniones de Clientes");

        try
        {
            var informe = await _runner.RunAsync(stoppingToken);

            Console.WriteLine(ReportPrinter.BuildExtractionSummary(informe.Extracciones));
            Console.WriteLine(ReportPrinter.BuildSummary(informe.Cargas));

            var detail = ReportPrinter.BuildRejectedDetail(informe.Cargas, maxPerTable: 15);
            if (!string.IsNullOrWhiteSpace(detail))
                Console.WriteLine(detail);

            var logPath = Path.Combine(AppContext.BaseDirectory, "etl_rejected_log.csv");
            await File.WriteAllTextAsync(logPath, ReportPrinter.BuildFullRejectedLog(informe.Cargas), stoppingToken);
            Console.WriteLine($"Log completo de registros rechazados: {logPath}");

            _logger.LogInformation("Proceso ETL finalizado.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error fatal durante la ejecución del proceso ETL");
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }
}
