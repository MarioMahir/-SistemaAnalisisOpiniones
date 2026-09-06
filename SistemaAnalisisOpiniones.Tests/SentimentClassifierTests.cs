using SistemaAnalisisOpiniones.Application;

namespace SistemaAnalisisOpiniones.Tests;

public class SentimentClassifierTests
{
    private readonly SentimentClassifier _classifier = new();

    [Theory]
    [InlineData("Me encanta este producto, excelente calidad.")]
    [InlineData("Producto llegó rápido y funciona perfecto.")]
    [InlineData("Gran relación calidad-precio")]
    [InlineData("Muy satisfecho con la compra, lo recomiendo.")]
    [InlineData("Calidad superior, muy contento")]
    [InlineData("El producto llegó en perfecto estado y antes de tiempo.")]
    public void Clasifica_ComoPositiva(string comentario)
    {
        Assert.Equal(SentimentClassifier.Positiva, _classifier.Clasificar(comentario));
    }

    [Theory]
    [InlineData("Muy mala calidad, se rompió rápido")]
    [InlineData("Pésima atención al cliente")]
    [InlineData("Envío tardío y producto dañado")]
    [InlineData("No cumple con lo anunciado, insatisfecho")]
    [InlineData("Mala calidad, no lo recomiendo para nada.")]
    [InlineData("Estoy decepcionado")]
    public void Clasifica_ComoNegativa(string comentario)
    {
        Assert.Equal(SentimentClassifier.Negativa, _classifier.Clasificar(comentario));
    }

    [Theory]
    [InlineData("Información suficiente, sin mayor novedad")]
    [InlineData("Entrega correcta, sin comentarios adicionales. OK.")]
    [InlineData("Producto recibido, cumple su función. OK.")]
    [InlineData("Satisface lo básico")]
    [InlineData("Ni malo ni excelente, simplemente regular.")]
    [InlineData("")]
    [InlineData(null)]
    public void Clasifica_ComoNeutra(string? comentario)
    {
        Assert.Equal(SentimentClassifier.Neutra, _classifier.Clasificar(comentario));
    }

    [Fact]
    public void LaNegacion_InvierteLaPolaridad()
    {
        Assert.Equal(SentimentClassifier.Positiva, _classifier.Clasificar("Lo recomiendo"));
        Assert.Equal(SentimentClassifier.Negativa, _classifier.Clasificar("No lo recomiendo"));
        Assert.Equal(SentimentClassifier.Negativa, _classifier.Clasificar("Nunca funciona"));
    }

    [Fact]
    public void IgnoraAcentosYMayusculas()
    {
        Assert.Equal(_classifier.Clasificar("PÉSIMA atención"), _classifier.Clasificar("pesima atencion"));
        Assert.Equal(SentimentClassifier.Negativa, _classifier.Clasificar("pesima atencion"));
    }
}
