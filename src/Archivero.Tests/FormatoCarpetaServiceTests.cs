using Archivero.Datos;
using Archivero.Servicios;

namespace Archivero.Tests;

public class FormatoCarpetaServiceTests : IDisposable
{
    private readonly string _carpetaTemporal;

    public FormatoCarpetaServiceTests()
    {
        _carpetaTemporal = Path.Combine(Path.GetTempPath(), "archivero-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_carpetaTemporal);
    }

    [Fact]
    public void Detectar_ConCarpetaVacia_DevuelveDirecto()
    {
        var resultado = FormatoCarpetaService.Detectar(_carpetaTemporal);

        Assert.Equal(FormatoCarpeta.Directo, resultado.Formato);
        Assert.Null(resultado.PatronCarpeta);
    }

    [Fact]
    public void Detectar_ConSubcarpetasDeAnioDeCuatroDigitos_DevuelvePatronYyyy()
    {
        Directory.CreateDirectory(Path.Combine(_carpetaTemporal, "2023"));
        Directory.CreateDirectory(Path.Combine(_carpetaTemporal, "2024"));
        Directory.CreateDirectory(Path.Combine(_carpetaTemporal, "2025"));

        var resultado = FormatoCarpetaService.Detectar(_carpetaTemporal);

        Assert.Equal(FormatoCarpeta.Anio, resultado.Formato);
        Assert.Equal("yyyy", resultado.PatronCarpeta);
    }

    [Fact]
    public void Detectar_ConSubcarpetasDeAnioDeDosDigitos_DevuelvePatronYy()
    {
        Directory.CreateDirectory(Path.Combine(_carpetaTemporal, "23"));
        Directory.CreateDirectory(Path.Combine(_carpetaTemporal, "24"));

        var resultado = FormatoCarpetaService.Detectar(_carpetaTemporal);

        Assert.Equal(FormatoCarpeta.Anio, resultado.Formato);
        Assert.Equal("yy", resultado.PatronCarpeta);
    }

    [Fact]
    public void Detectar_ConSubcarpetasDeAnioYMesNumerico_DevuelveAnioMesConPatronCombinado()
    {
        var carpetaAnio = Path.Combine(_carpetaTemporal, "2024");
        Directory.CreateDirectory(Path.Combine(carpetaAnio, "01"));
        Directory.CreateDirectory(Path.Combine(carpetaAnio, "02"));

        var resultado = FormatoCarpetaService.Detectar(_carpetaTemporal);

        Assert.Equal(FormatoCarpeta.AnioMes, resultado.Formato);
        Assert.Equal("yyyy\\MM", resultado.PatronCarpeta);
    }

    [Fact]
    public void Detectar_ConSubcarpetasQueNoSonFechas_DevuelveDirecto()
    {
        Directory.CreateDirectory(Path.Combine(_carpetaTemporal, "Facturas"));
        Directory.CreateDirectory(Path.Combine(_carpetaTemporal, "Recibos"));

        var resultado = FormatoCarpetaService.Detectar(_carpetaTemporal);

        Assert.Equal(FormatoCarpeta.Directo, resultado.Formato);
    }

    [Theory]
    [InlineData("yyyy", "2026")]
    [InlineData("yy", "26")]
    public void ConstruirSubcarpeta_ConFormatoAnio_DevuelveElAnioSegunElPatron(string patron, string esperado)
    {
        var resultado = FormatoCarpetaService.ConstruirSubcarpeta(FormatoCarpeta.Anio, patron, new DateTime(2026, 9, 12));

        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public void ConstruirSubcarpeta_ConFormatoAnioMes_DevuelveRutaConAnioYMes()
    {
        var resultado = FormatoCarpetaService.ConstruirSubcarpeta(FormatoCarpeta.AnioMes, "yyyy\\MM", new DateTime(2026, 9, 12));

        Assert.Equal("2026\\09", resultado);
    }

    [Fact]
    public void ConstruirSubcarpeta_ConFormatoDirecto_DevuelveVacio()
    {
        var resultado = FormatoCarpetaService.ConstruirSubcarpeta(FormatoCarpeta.Directo, null, DateTime.Now);

        Assert.Equal(string.Empty, resultado);
    }

    public void Dispose()
    {
        Directory.Delete(_carpetaTemporal, recursive: true);
    }
}
