namespace SistemaAnalisisOpiniones.Configuration;

/// <summary>
/// Configuración modular de las fuentes de datos del ETL.
/// Cada fuente puede habilitarse o deshabilitarse de forma independiente,
/// y agregar una fuente nueva solo requiere una nueva sección aquí y un
/// nuevo extractor que implemente IExtractor.
/// </summary>
public class FuentesOptions
{
    public CsvFuenteOptions Csv { get; set; } = new();
    public BaseDatosFuenteOptions BaseDatos { get; set; } = new();
    public ApiFuenteOptions Api { get; set; } = new();
}

public class CsvFuenteOptions
{
    public bool Enabled { get; set; } = true;
    public string Carpeta { get; set; } = "Data/Csv";
    public string ArchivoClientes { get; set; } = "clients.csv";
    public string ArchivoProductos { get; set; } = "products.csv";
    public string ArchivoFuentesDato { get; set; } = "fuente_datos.csv";
    public string ArchivoEncuestas { get; set; } = "surveys_part1.csv";
}

public class BaseDatosFuenteOptions
{
    public bool Enabled { get; set; } = true;
    public string ConnectionString { get; set; } = "";
    public string Query { get; set; } = "";
}

public class ApiFuenteOptions
{
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "";
    public string EndpointComentarios { get; set; } = "/api/comments";
    public int TimeoutSegundos { get; set; } = 30;
}
