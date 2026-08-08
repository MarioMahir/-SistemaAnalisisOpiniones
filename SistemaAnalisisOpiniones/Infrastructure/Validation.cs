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

    /// <summary>
    /// Homogeneiza los identificadores heterogéneos de las fuentes: las
    /// reseñas web y los comentarios sociales usan IDs con prefijo y ceros
    /// a la izquierda ('C007', 'P016') mientras los catálogos maestros usan
    /// numéricos planos ('7', '16'). Si el valor tiene formato letra(s) +
    /// dígitos se devuelve la parte numérica sin ceros a la izquierda; en
    /// cualquier otro caso se devuelve tal cual.
    /// </summary>
    public static string? NormalizarIdOrigen(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var texto = value.Trim();
        var digitos = 0;
        while (digitos < texto.Length && char.IsAsciiDigit(texto[texto.Length - 1 - digitos]))
            digitos++;

        if (digitos == 0 || digitos == texto.Length)
            return texto;

        var prefijo = texto[..^digitos];
        if (!prefijo.All(char.IsAsciiLetter))
            return texto;

        var numero = texto[^digitos..].TrimStart('0');
        return numero.Length == 0 ? "0" : numero;
    }
}
