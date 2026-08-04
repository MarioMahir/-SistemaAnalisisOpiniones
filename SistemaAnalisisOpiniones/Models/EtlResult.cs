namespace SistemaAnalisisOpiniones.Models;

public class RejectedRecord
{
    public int RowNumber { get; set; }
    public string? Key { get; set; }
    public string Reason { get; set; } = "";
}

public class EtlResult
{
    public string TableName { get; set; } = "";
    public int Processed { get; set; }
    public int Inserted { get; set; }
    public int Rejected => RejectedRecords.Count;
    public List<RejectedRecord> RejectedRecords { get; } = new();
}

public class EtlContext
{
    public HashSet<string> ClienteIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> ProductoIds { get; } = new(StringComparer.OrdinalIgnoreCase);
}
