using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SistemaAnalisisOpiniones.Application.Interfaces;

namespace SistemaAnalisisOpiniones.Application;

// Clasificador de sentimiento por palabras clave (enfoque sencillo exigido por el SRS).
// Cuenta términos positivos y negativos del comentario, invierte la polaridad cuando el
// término viene precedido por una negación ("no recomiendo", "no cumple") y decide por el
// signo del puntaje. Los acentos y las mayúsculas se ignoran para que "Pésima" y "pesima"
// se traten igual.
public class SentimentClassifier : ISentimentClassifier
{
    public const string Positiva = "Positiva";
    public const string Negativa = "Negativa";
    public const string Neutra = "Neutra";

    // Frases de varias palabras se evalúan primero, porque su significado no se deduce
    // de las palabras sueltas ("calidad-precio" es positivo aunque "precio" sea neutro).
    private static readonly (string Frase, int Puntos)[] Frases =
    {
        ("relacion calidad precio", 2),
        ("calidad superior", 2),
        ("funciona perfecto", 2),
        ("antes de tiempo", 1),
        ("perfecto estado", 2),
        ("no volveria a comprar", -2),
        ("se rompio", -2),
        ("no cumple", -2),
        ("no lo recomiendo", -3),
        ("no recomiendo", -3),
        ("nada excepcional", -1),
        ("ni malo ni excelente", 0),
        ("ni bueno ni malo", 0),
        ("ni malo ni bueno", 0),
        ("entrega correcta", 0),
        ("sin mayor novedad", 0),
        ("sin comentarios adicionales", 0),
        ("cumple su funcion", 0),
        ("satisface lo basico", 0),
        ("esperaba mas", -1),
    };

    private static readonly Dictionary<string, int> Palabras = new()
    {
        // Positivas
        ["excelente"] = 2, ["excelentes"] = 2, ["perfecto"] = 2, ["perfecta"] = 2,
        ["recomendable"] = 2, ["recomiendo"] = 2, ["encanta"] = 2, ["encanto"] = 2,
        ["genial"] = 2, ["increible"] = 2, ["fantastico"] = 2, ["maravilloso"] = 2,
        ["bueno"] = 1, ["buena"] = 1, ["buenos"] = 1, ["buenas"] = 1, ["bien"] = 1,
        ["rapido"] = 1, ["rapida"] = 1, ["contento"] = 2, ["contenta"] = 2, ["feliz"] = 2,
        ["satisfecho"] = 2, ["satisfecha"] = 2, ["gran"] = 1, ["calidad"] = 1,
        ["superior"] = 1, ["supero"] = 2, ["correcta"] = 1, ["correcto"] = 1,
        ["cumple"] = 1, ["funciona"] = 1, ["agradable"] = 1, ["util"] = 1,

        // Negativas
        ["malo"] = -2, ["mala"] = -2, ["malos"] = -2, ["malas"] = -2, ["mal"] = -2,
        ["pesimo"] = -3, ["pesima"] = -3, ["terrible"] = -3, ["horrible"] = -3,
        ["peor"] = -2, ["decepcion"] = -2, ["decepcionado"] = -2, ["decepcionada"] = -2,
        ["decepcionante"] = -2, ["insatisfecho"] = -2, ["insatisfecha"] = -2,
        ["rompio"] = -2, ["roto"] = -2, ["rota"] = -2, ["danado"] = -2, ["danada"] = -2,
        ["defectuoso"] = -2, ["defectuosa"] = -2, ["tardio"] = -1, ["tarde"] = -1,
        ["lento"] = -1, ["lenta"] = -1, ["fallo"] = -2, ["falla"] = -2, ["fraude"] = -3,
        ["estafa"] = -3, ["reclamo"] = -1, ["problema"] = -1, ["problemas"] = -1,
        ["nunca"] = -1, ["nada"] = -1, ["deficiente"] = -2, ["inaceptable"] = -3,

        // Neutras (se listan para que no cuenten aunque aparezcan junto a otras)
        ["regular"] = 0, ["normal"] = 0, ["ok"] = 0, ["basico"] = 0, ["suficiente"] = 0,
    };

    private static readonly HashSet<string> Negaciones = new() { "no", "nunca", "jamas", "tampoco", "ni" };

    private static readonly Regex Separador = new(@"[^a-z0-9]+", RegexOptions.Compiled);

    public string Clasificar(string? comentario)
    {
        if (string.IsNullOrWhiteSpace(comentario))
            return Neutra;

        var texto = Normalizar(comentario);
        var puntaje = 0;

        foreach (var (frase, puntos) in Frases)
        {
            if (!texto.Contains(frase, StringComparison.Ordinal))
                continue;

            puntaje += puntos;
            // La frase ya fue evaluada completa: se retira para no volver a puntuar sus palabras.
            texto = texto.Replace(frase, " ", StringComparison.Ordinal);
        }

        var tokens = Separador.Split(texto).Where(t => t.Length > 0).ToArray();
        for (var i = 0; i < tokens.Length; i++)
        {
            if (!Palabras.TryGetValue(tokens[i], out var puntos) || puntos == 0)
                continue;

            var negado = (i >= 1 && Negaciones.Contains(tokens[i - 1])) ||
                         (i >= 2 && Negaciones.Contains(tokens[i - 2]));
            puntaje += negado ? -puntos : puntos;
        }

        return puntaje > 0 ? Positiva : puntaje < 0 ? Negativa : Neutra;
    }

    // Minúsculas, sin acentos ni diéresis, y con guiones convertidos en espacios.
    private static string Normalizar(string texto)
    {
        var descompuesto = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);
        foreach (var c in descompuesto)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(c);
            if (categoria == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(c == '-' || c == '_' ? ' ' : c);
        }
        return " " + sb.ToString().Normalize(NormalizationForm.FormC) + " ";
    }
}
