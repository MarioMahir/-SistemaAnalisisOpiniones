namespace SistemaAnalisisOpiniones.Infrastructure;

public class EtlOptions
{
    public string ConnectionString { get; set; } = "";
    public string DataFolder { get; set; } = "Data/Csv";
    public int MaxRejectedShownPerTable { get; set; } = 15;
}
