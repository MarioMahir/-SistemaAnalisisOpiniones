namespace SistemaAnalisisOpiniones.Configuration;

/// <summary>Configuración general del proceso ETL (destino staging y reporte).</summary>
public class EtlOptions
{
    public string ConnectionString { get; set; } = "";
    public int MaxRejectedShownPerTable { get; set; } = 15;
}
