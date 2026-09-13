using Archivero.Datos;

namespace Archivero.Tests;

public class ConfiguracionDocumentoRepositoryTests : IDisposable
{
    private readonly string _rutaDbTemporal;

    public ConfiguracionDocumentoRepositoryTests()
    {
        _rutaDbTemporal = Path.Combine(Path.GetTempPath(), $"archivero-tests-{Guid.NewGuid():N}.db");
        BaseDeDatos.RutaArchivo = _rutaDbTemporal;
        BaseDeDatos.AsegurarEsquema();
    }

    [Fact]
    public void ExisteCoincidenciaExacta_ConEmisorYTipoYaGuardados_DevuelveTrue()
    {
        var repo = new ConfiguracionDocumentoRepository();
        repo.GuardarNueva("Banco Galicia", "Resumen de cuenta", @"C:\Destino", FormatoCarpeta.Directo, null, false, new List<Marca>());

        Assert.True(repo.ExisteCoincidenciaExacta("Banco Galicia", "Resumen de cuenta"));
    }

    [Fact]
    public void ExisteCoincidenciaExacta_ConEmisorDistinto_DevuelveFalse()
    {
        var repo = new ConfiguracionDocumentoRepository();
        repo.GuardarNueva("Banco Galicia", "Resumen de cuenta", @"C:\Destino", FormatoCarpeta.Directo, null, false, new List<Marca>());

        Assert.False(repo.ExisteCoincidenciaExacta("Banco Nacion", "Resumen de cuenta"));
    }

    [Fact]
    public void ExisteCoincidenciaExacta_ConTipoDistinto_DevuelveFalse()
    {
        var repo = new ConfiguracionDocumentoRepository();
        repo.GuardarNueva("Banco Galicia", "Resumen de cuenta", @"C:\Destino", FormatoCarpeta.Directo, null, false, new List<Marca>());

        Assert.False(repo.ExisteCoincidenciaExacta("Banco Galicia", "Factura"));
    }

    [Fact]
    public void ExisteCoincidenciaExacta_SinNingunaConfiguracionGuardada_DevuelveFalse()
    {
        var repo = new ConfiguracionDocumentoRepository();

        Assert.False(repo.ExisteCoincidenciaExacta("Cualquiera", "Cualquiera"));
    }

    [Fact]
    public void BuscarPorEmisorYTipo_DevuelveElPatronConLasMarcasGuardadas()
    {
        var repo = new ConfiguracionDocumentoRepository();
        var marcas = new List<Marca>
        {
            new(CampoMarca.Emisor, 0, 0.1, 0.1, 0.2, 0.05),
            new(CampoMarca.Tipo, 0, 0.1, 0.2, 0.2, 0.05)
        };
        repo.GuardarNueva("Banco Galicia", "Resumen de cuenta", @"C:\Destino", FormatoCarpeta.Anio, "yyyy", true, marcas);

        var configuracion = repo.BuscarPorEmisorYTipo("Banco Galicia", "Resumen de cuenta");

        Assert.NotNull(configuracion);
        var patron = Assert.Single(configuracion!.Patrones);
        Assert.Equal(2, patron.Marcas.Count);
        Assert.Contains(patron.Marcas, m => m.Campo == CampoMarca.Emisor);
        Assert.Contains(patron.Marcas, m => m.Campo == CampoMarca.Tipo);
    }

    [Fact]
    public void AgregarPatronAConfiguracionExistente_SumaUnSegundoPatron()
    {
        var repo = new ConfiguracionDocumentoRepository();
        var configuracionId = repo.GuardarNueva(
            "Banco Galicia", "Resumen de cuenta", @"C:\Destino", FormatoCarpeta.Directo, null, false,
            [new Marca(CampoMarca.Emisor, 0, 0.1, 0.1, 0.2, 0.05), new Marca(CampoMarca.Tipo, 0, 0.1, 0.2, 0.2, 0.05)]);

        repo.AgregarPatronAConfiguracionExistente(
            configuracionId,
            [new Marca(CampoMarca.Emisor, 0, 0.3, 0.3, 0.2, 0.05), new Marca(CampoMarca.Tipo, 0, 0.3, 0.4, 0.2, 0.05)]);

        var configuracion = repo.BuscarPorEmisorYTipo("Banco Galicia", "Resumen de cuenta");

        Assert.NotNull(configuracion);
        Assert.Equal(2, configuracion!.Patrones.Count);
    }

    public void Dispose()
    {
        // Microsoft.Data.Sqlite reutiliza handles nativos por cadena de conexion (pooling);
        // hay que vaciar el pool antes de borrar el archivo o queda "en uso".
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        if (File.Exists(_rutaDbTemporal))
        {
            File.Delete(_rutaDbTemporal);
        }
    }
}
