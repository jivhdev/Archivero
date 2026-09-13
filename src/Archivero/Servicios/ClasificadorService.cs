using System.IO;
using System.Security.Cryptography;
using Archivero.Datos;

namespace Archivero.Servicios;

public class ArchivoDuplicadoException : Exception
{
    public string RutaDestino { get; }

    public ArchivoDuplicadoException(string rutaDestino)
        : base($"Ya existe un archivo en \"{rutaDestino}\".")
    {
        RutaDestino = rutaDestino;
    }
}

public static class ClasificadorService
{
    /// <summary>
    /// Calcula dónde iría el archivo (carpeta de fecha + nombre según la configuración), sin
    /// tocar el disco. Se usa tanto para clasificar como para saber, ante un duplicado, qué
    /// ruta exacta está en conflicto (REQ-002).
    /// </summary>
    public static string CalcularRutaDestino(
        string rutaArchivoOrigen,
        ConfiguracionDocumento configuracion,
        DateTime? fechaExtraida,
        string? nombreExtraido)
    {
        var subcarpeta = FormatoCarpetaService.ConstruirSubcarpeta(
            configuracion.FormatoCarpeta, configuracion.PatronCarpeta, fechaExtraida ?? DateTime.Now);

        var carpetaFinal = string.IsNullOrEmpty(subcarpeta)
            ? configuracion.CarpetaDestino
            : Path.Combine(configuracion.CarpetaDestino, subcarpeta);

        var nombreArchivo = configuracion.Renombrar && !string.IsNullOrWhiteSpace(nombreExtraido)
            ? $"{nombreExtraido}{Path.GetExtension(rutaArchivoOrigen)}"
            : Path.GetFileName(rutaArchivoOrigen);

        return Path.Combine(carpetaFinal, nombreArchivo);
    }

    public static string Clasificar(
        string rutaArchivoOrigen,
        ConfiguracionDocumento configuracion,
        DateTime? fechaExtraida,
        string? nombreExtraido)
    {
        var rutaDestino = CalcularRutaDestino(rutaArchivoOrigen, configuracion, fechaExtraida, nombreExtraido);
        Directory.CreateDirectory(Path.GetDirectoryName(rutaDestino)!);

        if (File.Exists(rutaDestino))
        {
            throw new ArchivoDuplicadoException(rutaDestino);
        }

        CopiarVerificarBorrar(rutaArchivoOrigen, rutaDestino);
        return rutaDestino;
    }

    /// <summary>Resolución de duplicado (REQ-002), opción "Reemplazar": borra lo que había y guarda lo nuevo.</summary>
    public static void ReemplazarYClasificar(string rutaArchivoOrigen, string rutaDestino)
    {
        File.Delete(rutaDestino);
        CopiarVerificarBorrar(rutaArchivoOrigen, rutaDestino);
    }

    /// <summary>Resolución de duplicado (REQ-002), opción "Guardar en otra ubicación como excepción".</summary>
    public static void GuardarComoExcepcion(string rutaArchivoOrigen, string rutaDestino)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(rutaDestino)!);

        if (File.Exists(rutaDestino))
        {
            throw new ArchivoDuplicadoException(rutaDestino);
        }

        CopiarVerificarBorrar(rutaArchivoOrigen, rutaDestino);
    }

    private static void CopiarVerificarBorrar(string origen, string destino)
    {
        File.Copy(origen, destino);

        if (!ArchivosSonIdenticos(origen, destino))
        {
            File.Delete(destino);
            throw new IOException("La copia no coincide con el original; no se borró el archivo original.");
        }

        File.Delete(origen);
    }

    private static bool ArchivosSonIdenticos(string rutaA, string rutaB)
    {
        using var streamA = File.OpenRead(rutaA);
        using var streamB = File.OpenRead(rutaB);

        var hashA = SHA256.HashData(streamA);
        var hashB = SHA256.HashData(streamB);

        return hashA.AsSpan().SequenceEqual(hashB);
    }
}
