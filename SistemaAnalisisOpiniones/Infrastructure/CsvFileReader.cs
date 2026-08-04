using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace SistemaAnalisisOpiniones.Infrastructure;

public static class CsvFileReader
{
    public static List<T> Read<T>(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"No se encontró el archivo de origen: {path}", path);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim,
            DetectDelimiter = false,
        };

        using var streamReader = new StreamReader(path, detectEncodingFromByteOrderMarks: true);
        using var csv = new CsvReader(streamReader, config);
        return csv.GetRecords<T>().ToList();
    }
}
