using Archivero.Datos;
using Archivero.Servicios;

namespace Archivero.Tests;

public class ClasificadorServiceTests : IDisposable
{
    private readonly string _raiz;
    private readonly string _carpetaOrigen;
    private readonly string _carpetaDestino;

    public ClasificadorServiceTests()
    {
        _raiz = Path.Combine(Path.GetTempPath(), "archivero-tests-" + Guid.NewGuid().ToString("N"));
        _carpetaOrigen = Path.Combine(_raiz, "origen");
        _carpetaDestino = Path.Combine(_raiz, "destino");
        Directory.CreateDirectory(_carpetaOrigen);
        Directory.CreateDirectory(_carpetaDestino);
    }

    private ConfiguracionDocumento Configuracion() => new()
    {
        Id = 1,
        Emisor = "Banco de Prueba SA",
        Tipo = "Resumen de cuenta",
        CarpetaDestino = _carpetaDestino,
        FormatoCarpeta = FormatoCarpeta.Directo,
        PatronCarpeta = null,
        Renombrar = false,
        Patrones = []
    };

    private string CrearArchivo(string carpeta, string nombre, string contenido = "contenido")
    {
        var ruta = Path.Combine(carpeta, nombre);
        File.WriteAllText(ruta, contenido);
        return ruta;
    }

    [Fact]
    public void CalcularRutaDestino_NoTocaElDisco_SoloCalcula()
    {
        var origen = CrearArchivo(_carpetaOrigen, "factura.pdf");

        var ruta = ClasificadorService.CalcularRutaDestino(origen, Configuracion(), null, null);

        Assert.Equal(Path.Combine(_carpetaDestino, "factura.pdf"), ruta);
        Assert.True(File.Exists(origen), "CalcularRutaDestino no debería mover ni borrar nada");
    }

    [Fact]
    public void Clasificar_ConArchivoYaExistenteEnDestino_LanzaArchivoDuplicadoException()
    {
        var origen = CrearArchivo(_carpetaOrigen, "factura.pdf", "nuevo");
        CrearArchivo(_carpetaDestino, "factura.pdf", "viejo");

        var ex = Assert.Throws<ArchivoDuplicadoException>(() =>
            ClasificadorService.Clasificar(origen, Configuracion(), null, null));

        Assert.Equal(Path.Combine(_carpetaDestino, "factura.pdf"), ex.RutaDestino);
        Assert.True(File.Exists(origen), "El original no se debe tocar ante un duplicado");
    }

    [Fact]
    public void ReemplazarYClasificar_BorraElExistenteYGuardaElNuevo()
    {
        var origen = CrearArchivo(_carpetaOrigen, "factura.pdf", "contenido nuevo");
        var destino = CrearArchivo(_carpetaDestino, "factura.pdf", "contenido viejo");

        ClasificadorService.ReemplazarYClasificar(origen, destino);

        Assert.False(File.Exists(origen));
        Assert.True(File.Exists(destino));
        Assert.Equal("contenido nuevo", File.ReadAllText(destino));
    }

    [Fact]
    public void GuardarComoExcepcion_GuardaEnLaCarpetaElegidaConElMismoNombre()
    {
        var origen = CrearArchivo(_carpetaOrigen, "factura.pdf");
        var carpetaExcepcion = Path.Combine(_raiz, "excepcion");
        var rutaExcepcion = Path.Combine(carpetaExcepcion, "factura.pdf");

        ClasificadorService.GuardarComoExcepcion(origen, rutaExcepcion);

        Assert.False(File.Exists(origen));
        Assert.True(File.Exists(rutaExcepcion));
    }

    [Fact]
    public void GuardarComoExcepcion_SiTambienExisteAhi_LanzaArchivoDuplicadoException()
    {
        var origen = CrearArchivo(_carpetaOrigen, "factura.pdf");
        var carpetaExcepcion = Path.Combine(_raiz, "excepcion");
        Directory.CreateDirectory(carpetaExcepcion);
        var rutaExcepcion = CrearArchivo(carpetaExcepcion, "factura.pdf");

        Assert.Throws<ArchivoDuplicadoException>(() =>
            ClasificadorService.GuardarComoExcepcion(origen, rutaExcepcion));

        Assert.True(File.Exists(origen), "El original no se debe tocar si tambien hay duplicado en la excepcion");
    }

    public void Dispose()
    {
        Directory.Delete(_raiz, recursive: true);
    }
}
