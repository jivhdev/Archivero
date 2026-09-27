using Archivero.Servicios;

namespace Archivero.Tests;

public class LimpiezaNombreServiceTests
{
    [Theory]
    [InlineData("00123", "123")]
    [InlineData("0001200", "1200")]
    [InlineData("0000000042", "42")]
    public void QuitarCerosIzquierda_ConCerosDeRelleno_LosQuita(string nombre, string esperado)
    {
        Assert.Equal(esperado, LimpiezaNombreService.QuitarCerosIzquierda(nombre));
    }

    [Theory]
    [InlineData("1200")]
    [InlineData("123")]
    [InlineData("100")]
    public void QuitarCerosIzquierda_SinCerosALaIzquierda_NoTocaLosCerosDelValor(string nombre)
    {
        Assert.Equal(nombre, LimpiezaNombreService.QuitarCerosIzquierda(nombre));
    }

    [Fact]
    public void QuitarCerosIzquierda_SoloCeros_DejaUnCero()
    {
        Assert.Equal("0", LimpiezaNombreService.QuitarCerosIzquierda("0000"));
    }

    [Theory]
    [InlineData("Guia 00123")]
    [InlineData("00123-A")]
    [InlineData("")]
    public void QuitarCerosIzquierda_NombreNoPuramenteNumerico_LoDejaTalCual(string nombre)
    {
        Assert.Equal(nombre, LimpiezaNombreService.QuitarCerosIzquierda(nombre));
    }

    [Theory]
    [InlineData("Guia N° 00123-2026", "001232026")]
    [InlineData("sin numeros", "")]
    public void DejarSoloNumeros_ConservaSoloLosDigitos(string nombre, string esperado)
    {
        Assert.Equal(esperado, LimpiezaNombreService.DejarSoloNumeros(nombre));
    }
}
