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
    public static string Clasificar(
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

        Directory.CreateDirectory(carpetaFinal);

        var nombreArchivo = configuracion.Renombrar && !string.IsNullOrWhiteSpace(nombreExtraido)
            ? $"{nombreExtraido}{Path.GetExtension(rutaArchivoOrigen)}"
            : Path.GetFileName(rutaArchivoOrigen);

        var rutaDestino = Path.Combine(carpetaFinal, nombreArchivo);

        if (File.Exists(rutaDestino))
        {
            throw new ArchivoDuplicadoException(rutaDestino);
        }

        CopiarVerificarBorrar(rutaArchivoOrigen, rutaDestino);
        return rutaDestino;
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
