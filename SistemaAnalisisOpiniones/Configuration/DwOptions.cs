namespace SistemaAnalisisOpiniones.Configuration;

/// <summary>Configuración de la carga hacia el Data Warehouse analítico.</summary>
public class DwOptions
{
    public bool Enabled { get; set; } = true;
    public string ConnectionString { get; set; } = "";
}
