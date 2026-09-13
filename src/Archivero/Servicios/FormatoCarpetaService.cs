using System.Globalization;
using System.IO;
using Archivero.Datos;

namespace Archivero.Servicios;

public record DeteccionFormatoCarpeta(FormatoCarpeta Formato, string? PatronCarpeta);

public static class FormatoCarpetaService
{
    private static readonly string[] PatronesAnioConocidos = ["yyyy", "yy"];
    private static readonly string[] PatronesMesConocidos = ["MM", "MMMM"];
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-ES");

    public static DeteccionFormatoCarpeta Detectar(string carpetaDestino)
    {
        if (!Directory.Exists(carpetaDestino))
        {
            return new DeteccionFormatoCarpeta(FormatoCarpeta.Directo, null);
        }

        var subcarpetas = Directory.GetDirectories(carpetaDestino);
        var nombresAnio = subcarpetas.Select(Path.GetFileName).OfType<string>().ToList();

        var patronAnio = DetectarPatron(nombresAnio, PatronesAnioConocidos);
        if (patronAnio is null)
        {
            return new DeteccionFormatoCarpeta(FormatoCarpeta.Directo, null);
        }

        var primeraCarpetaAnio = subcarpetas.First(d => CoincideConPatron(Path.GetFileName(d), patronAnio));
        var nombresMes = Directory.GetDirectories(primeraCarpetaAnio).Select(Path.GetFileName).OfType<string>().ToList();

        var patronMes = nombresMes.Count > 0 ? DetectarPatron(nombresMes, PatronesMesConocidos) : null;

        return patronMes is not null
            ? new DeteccionFormatoCarpeta(FormatoCarpeta.AnioMes, $"{patronAnio}\\{patronMes}")
            : new DeteccionFormatoCarpeta(FormatoCarpeta.Anio, patronAnio);
    }

    private static string? DetectarPatron(List<string> nombres, string[] patronesConocidos)
    {
        if (nombres.Count == 0)
        {
            return null;
        }

        foreach (var patron in patronesConocidos)
        {
            if (nombres.All(nombre => CoincideConPatron(nombre, patron)))
            {
                return patron;
            }
        }

        return null;
    }

    public static bool CoincideConPatron(string? nombre, string patron) =>
        nombre is not null && DateTime.TryParseExact(nombre, patron, Cultura, DateTimeStyles.None, out _);

    public static string ConstruirSubcarpeta(FormatoCarpeta formato, string? patron, DateTime fecha)
    {
        if (formato == FormatoCarpeta.Directo)
        {
            return string.Empty;
        }

        if (patron is null)
        {
            throw new InvalidOperationException("Falta el patrón de carpeta.");
        }

        // Cada nivel de carpeta se formatea por separado y se combina con Path.Combine:
        // un "\" dentro de un formato de DateTime.ToString se interpreta como caracter de
        // escape, no como separador de ruta, asi que no se puede pasar "yyyy\MM" de una.
        var partesFormateadas = patron.Split('\\').Select(parte => fecha.ToString(parte, Cultura));
        return Path.Combine(partesFormateadas.ToArray());
    }
}
