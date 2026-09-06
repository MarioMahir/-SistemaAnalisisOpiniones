using SistemaAnalisisOpiniones.Infrastructure;

namespace SistemaAnalisisOpiniones.Tests;

public class ValidationTests
{
    [Theory]
    [InlineData("C007", "7")]
    [InlineData("P016", "16")]
    [InlineData("W0100", "100")]
    [InlineData("C0", "0")]
    [InlineData("12", "12")]
    [InlineData("  C019 ", "19")]
    [InlineData("AB12C", "AB12C")]
    [InlineData("SinNumero", "SinNumero")]
    public void NormalizarIdOrigen_QuitaPrefijoYCerosALaIzquierda(string entrada, string esperado)
    {
        Assert.Equal(esperado, Validation.NormalizarIdOrigen(entrada));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizarIdOrigen_DevuelveElValorVacioSinCambios(string? entrada)
    {
        Assert.Equal(entrada, Validation.NormalizarIdOrigen(entrada));
    }

    [Theory]
    [InlineData("2025-06-15", true)]
    [InlineData("15/06/2025", false)]
    [InlineData("no es fecha", false)]
    public void TryParseDate_AceptaSoloFormatosInvariantes(string entrada, bool esperado)
    {
        Assert.Equal(esperado, Validation.TryParseDate(entrada, out _));
    }

    [Theory]
    [InlineData("abc", 5, true)]
    [InlineData("abcdef", 5, false)]
    [InlineData(null, 5, true)]
    public void FitsLength_RespetaElMaximo(string? valor, int maximo, bool esperado)
    {
        Assert.Equal(esperado, Validation.FitsLength(valor, maximo));
    }
}
