namespace SistemaAnalisisOpiniones.Application.Interfaces;

public interface ISentimentClassifier
{
    // Clasifica un comentario libre como "Positiva", "Negativa" o "Neutra".
    string Clasificar(string? comentario);
}
