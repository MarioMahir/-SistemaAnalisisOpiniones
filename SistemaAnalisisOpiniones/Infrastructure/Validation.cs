using System.Globalization;

namespace SistemaAnalisisOpiniones.Infrastructure;

public static class Validation
{
    public static bool IsRequired(string? value) => !string.IsNullOrWhiteSpace(value);

    public static bool FitsLength(string? value, int maxLength) =>
        value is null || value.Length <= maxLength;

    public static bool TryParseDate(string? value, out DateTime date) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    public static bool TryParseInt(string? value, out int result) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
}
