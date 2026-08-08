namespace SistemaAnalisisOpiniones.Configuration;

public class EtlOptions
{
    public string ConnectionString { get; set; } = "";
    public int MaxRejectedShownPerTable { get; set; } = 15;
}
