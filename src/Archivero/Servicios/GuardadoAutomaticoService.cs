using System.IO;
using Archivero.Datos;
using Archivero.Servicios.Pdf;

namespace Archivero.Servicios;

public enum ResultadoGuardadoAutomatico
{
    Guardado,
    ValorInvalido,
    Duplicado,
    CarpetaNoDisponible
}

public record ResultadoProcesamiento(ResultadoGuardadoAutomatico Resultado, string? RutaFinal = null, string? Detalle = null);

public static class GuardadoAutomaticoService
{
    /// <summary>
    /// Extrae Fecha y/o Nombre de archivo (según lo que pida la configuración) usando el
    /// patrón que ya coincidió, valida que tengan una forma válida, y clasifica el archivo.
    /// </summary>
    public static ResultadoProcesamiento Procesar(string rutaArchivo, ConfiguracionDocumento configuracionConPatronCoincidente)
    {
        var marcas = configuracionConPatronCoincidente.Patrones.Single().Marcas;

        DateTime? fecha = null;
        if (configuracionConPatronCoincidente.FormatoCarpeta != FormatoCarpeta.Directo)
        {
            var marcaFecha = marcas.FirstOrDefault(m => m.Campo == CampoMarca.Fecha);
            if (marcaFecha is null)
            {
                return new ResultadoProcesamiento(ResultadoGuardadoAutomatico.ValorInvalido, Detalle: "El patrón no tiene marca de Fecha.");
            }

            var textoFecha = LectorPdf.ExtraerTexto(rutaArchivo, marcaFecha.Pagina, ARect(marcaFecha));
            if (!FechaExtraidaService.TryParsear(textoFecha, out var fechaParseada))
            {
                return new ResultadoProcesamiento(ResultadoGuardadoAutomatico.ValorInvalido, Detalle: $"Fecha extraída inválida: \"{textoFecha}\".");
            }

            fecha = fechaParseada;
        }

        string? nombreExtraido = null;
        if (configuracionConPatronCoincidente.Renombrar)
        {
            var marcaNombre = marcas.FirstOrDefault(m => m.Campo == CampoMarca.NombreArchivo);
            if (marcaNombre is null)
            {
                return new ResultadoProcesamiento(ResultadoGuardadoAutomatico.ValorInvalido, Detalle: "El patrón no tiene marca de Nombre de archivo.");
            }

            nombreExtraido = LectorPdf.ExtraerTexto(rutaArchivo, marcaNombre.Pagina, ARect(marcaNombre));
            if (string.IsNullOrWhiteSpace(nombreExtraido))
            {
                return new ResultadoProcesamiento(ResultadoGuardadoAutomatico.ValorInvalido, Detalle: "No se pudo extraer un nombre de archivo válido.");
            }
        }

        try
        {
            var rutaFinal = ClasificadorService.Clasificar(rutaArchivo, configuracionConPatronCoincidente, fecha, nombreExtraido);
            return new ResultadoProcesamiento(ResultadoGuardadoAutomatico.Guardado, rutaFinal);
        }
        catch (ArchivoDuplicadoException ex)
        {
            return new ResultadoProcesamiento(ResultadoGuardadoAutomatico.Duplicado, Detalle: ex.Message);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new ResultadoProcesamiento(ResultadoGuardadoAutomatico.CarpetaNoDisponible, Detalle: ex.Message);
        }
    }

    private static RectanguloFraccion ARect(Marca marca) => new(marca.X, marca.Y, marca.Ancho, marca.Alto);
}
