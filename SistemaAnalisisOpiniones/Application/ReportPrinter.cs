using System.Text;
using SistemaAnalisisOpiniones.Domain.Models;

namespace SistemaAnalisisOpiniones.Application;

public static class ReportPrinter
{
    public static string BuildExtractionSummary(IReadOnlyList<ResultadoExtraccion> extracciones)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("========================================================================");
        sb.AppendLine("                    RESUMEN DE LA FASE DE EXTRACCIÓN");
        sb.AppendLine("========================================================================");
        sb.AppendLine($"{"Fuente",-38}{"Registros",12}{"Duración",12}{"Estado",10}");
        sb.AppendLine(new string('-', 72));

        foreach (var e in extracciones)
        {
            var estado = e.Exitoso ? "OK" : "ERROR";
            sb.AppendLine($"{e.Fuente,-38}{e.RegistrosExtraidos,12}{e.DuracionMs + " ms",12}{estado,10}");
            if (!e.Exitoso && !string.IsNullOrWhiteSpace(e.Error))
                sb.AppendLine($"    Motivo: {e.Error}");
        }

        sb.AppendLine("========================================================================");
        return sb.ToString();
    }

    public static string BuildDwSummary(IReadOnlyList<ResultadoCargaDimension> cargas)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("========================================================================");
        sb.AppendLine("            RESUMEN DE LA CARGA DE DIMENSIONES DEL DW");
        sb.AppendLine("========================================================================");
        sb.AppendLine($"{"Dimensión",-20}{"Leídos",10}{"Insertados",12}{"Existentes",12}{"Duración",10}{"Estado",8}");
        sb.AppendLine(new string('-', 72));

        foreach (var c in cargas)
        {
            var estado = c.Exitoso ? "OK" : "ERROR";
            sb.AppendLine($"{c.Dimension,-20}{c.Leidos,10}{c.Insertados,12}{c.Existentes,12}{c.DuracionMs + " ms",10}{estado,8}");
            if (!c.Exitoso && !string.IsNullOrWhiteSpace(c.Error))
                sb.AppendLine($"    Motivo: {c.Error}");
        }

        sb.AppendLine("========================================================================");
        return sb.ToString();
    }

    public static string BuildSummary(IReadOnlyList<EtlResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("========================================================================");
        sb.AppendLine("                    RESUMEN DE LA CARGA AL STAGING");
        sb.AppendLine("        Sistema de Análisis de Opiniones de Clientes");
        sb.AppendLine("========================================================================");
        sb.AppendLine($"{"Tabla",-22}{"Procesados",12}{"Insertados",12}{"Rechazados",12}");
        sb.AppendLine(new string('-', 72));

        int totalProcessed = 0, totalInserted = 0, totalRejected = 0;
        foreach (var r in results)
        {
            sb.AppendLine($"{r.TableName,-22}{r.Processed,12}{r.Inserted,12}{r.Rejected,12}");
            totalProcessed += r.Processed;
            totalInserted += r.Inserted;
            totalRejected += r.Rejected;
        }

        sb.AppendLine(new string('-', 72));
        sb.AppendLine($"{"TOTAL",-22}{totalProcessed,12}{totalInserted,12}{totalRejected,12}");
        sb.AppendLine("========================================================================");
        return sb.ToString();
    }

    public static string BuildRejectedDetail(IReadOnlyList<EtlResult> results, int maxPerTable)
    {
        var sb = new StringBuilder();
        foreach (var r in results.Where(r => r.RejectedRecords.Count > 0))
        {
            sb.AppendLine($"--- Rechazados en {r.TableName} ({r.RejectedRecords.Count}) ---");
            foreach (var rec in r.RejectedRecords.Take(maxPerTable))
                sb.AppendLine($"  Fila {rec.RowNumber} [{rec.Key}]: {rec.Reason}");

            if (r.RejectedRecords.Count > maxPerTable)
                sb.AppendLine($"  ... y {r.RejectedRecords.Count - maxPerTable} más (ver log completo: etl_rejected_log.csv)");

            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static string BuildFullRejectedLog(IReadOnlyList<EtlResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Tabla,Fila,Clave,Motivo");
        foreach (var r in results)
        {
            foreach (var rec in r.RejectedRecords)
            {
                var motivo = rec.Reason.Replace("\"", "'");
                sb.AppendLine($"{r.TableName},{rec.RowNumber},\"{rec.Key}\",\"{motivo}\"");
            }
        }

        return sb.ToString();
    }
}
