using System.Globalization;
using System.IO;
using Archivero.Datos;

namespace Archivero.Servicios;

public record DeteccionFormatoCarpeta(FormatoCarpeta Formato, string? PatronCarpeta);

public static class FormatoCarpetaService
{
    private static readonly string[] TokensAnio = ["yyyy", "yy"];
    // "MMM" (abreviado, ej. "ene.") queda afuera: en es-ES el nombre abreviado termina en punto,
    // y Windows recorta el punto final de cualquier nombre de carpeta -- nunca podria coincidir
    // con una carpeta real.
    private static readonly string[] TokensMes = ["MM", "MMMM", "yyyyMM"];
    private static readonly string[] TodosLosTokens = TokensAnio.Concat(TokensMes).ToArray();
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-ES");

    public static DeteccionFormatoCarpeta Detectar(string carpetaDestino)
    {
        if (!Directory.Exists(carpetaDestino))
        {
            return new DeteccionFormatoCarpeta(FormatoCarpeta.Directo, null);
        }

        var carpetasNivel1 = Directory.GetDirectories(carpetaDestino);
        var nombresNivel1 = carpetasNivel1.Select(Path.GetFileName).OfType<string>().ToList();

        var patronNivel1 = DetectarPatronPorEvidencia(nombresNivel1, TokensAnio);
        if (patronNivel1 is null)
        {
            // Sin evidencia clara y consistente en las subcarpetas ya existentes: nunca se
            // inventa una subdivision que nadie pidio. Ver Caso-1 (preguntas/Caso-1.md), punto 2.
            return new DeteccionFormatoCarpeta(FormatoCarpeta.Directo, null);
        }

        // La evidencia del segundo nivel se junta de TODAS las carpetas de primer nivel que ya
        // coinciden con el patron (no solo la primera): con mas de un ejemplo real, se puede
        // distinguir un texto realmente fijo de un numero que solo parece fijo porque por ahora
        // hay una sola carpeta de primer nivel (ver DetectarPatronPorEvidencia).
        var nombresNivel2 = carpetasNivel1
            .Where(d => CoincideConPatron(Path.GetFileName(d), patronNivel1))
            .SelectMany(Directory.GetDirectories)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Distinct()
            .ToList();

        var patronNivel2 = DetectarPatronPorEvidencia(nombresNivel2, TokensMes);

        return patronNivel2 is not null
            ? new DeteccionFormatoCarpeta(FormatoCarpeta.AnioMes, $"{patronNivel1}\\{patronNivel2}")
            : new DeteccionFormatoCarpeta(FormatoCarpeta.Anio, patronNivel1);
    }

    /// <summary>
    /// Busca un patron real comparando los nombres entre si (evidencia), en vez de compararlos
    /// contra una lista fija de formatos completos. Separa, para cada nombre, una parte literal
    /// fija (igual en todos los ejemplos) de una parte variable, y prueba si esa parte variable
    /// es consistente con alguno de los tokens de fecha candidatos. Si no hay ningun nombre, o
    /// ninguna combinacion es consistente en TODOS los ejemplos, no hay evidencia: devuelve null
    /// (nunca se inventa un patron sin evidencia clara).
    /// </summary>
    private static string? DetectarPatronPorEvidencia(List<string> nombres, string[] tokensCandidatos)
    {
        if (nombres.Count == 0)
        {
            return null;
        }

        var largoMinimo = nombres.Min(n => n.Length);
        var prefijoMaximo = LargoPrefijoComun(nombres);
        var sufijoMaximo = LargoSufijoComun(nombres);

        foreach (var token in tokensCandidatos)
        {
            // Se prueba de mayor a menor cuanto texto literal se "come" el prefijo/sufijo: el
            // primer corte que da una parte variable consistente con el token, en los tres
            // (prefijo, variable, sufijo), es el patron real. Empezar por el corte mas grande
            // evita que un numero coincidencialmente compartido entre los ejemplos (ej. "202" en
            // "2023"/"2024"/"2025") se coma de mas por casualidad -- si eso pasara, el ancho de
            // la parte variable resultante no encajaria con el token y el corte se descarta.
            for (var prefijo = prefijoMaximo; prefijo >= 0; prefijo--)
            {
                for (var sufijo = sufijoMaximo; sufijo >= 0; sufijo--)
                {
                    if (prefijo + sufijo >= largoMinimo)
                    {
                        continue;
                    }

                    if (EncajaCorte(nombres, prefijo, sufijo, token))
                    {
                        var textoPrefijo = nombres[0][..prefijo];
                        var textoSufijo = sufijo == 0 ? string.Empty : nombres[0][^sufijo..];
                        return EnvolverLiteral(textoPrefijo) + token + EnvolverLiteral(textoSufijo);
                    }
                }
            }
        }

        return null;
    }

    private static bool EncajaCorte(List<string> nombres, int prefijo, int sufijo, string token)
    {
        foreach (var nombre in nombres)
        {
            if (nombre.Length < prefijo + sufijo)
            {
                return false;
            }

            var variable = nombre[prefijo..(nombre.Length - sufijo)];
            if (variable.Length == 0 || !CoincideConPatron(variable, token))
            {
                return false;
            }
        }

        // El prefijo y el sufijo deben ser iguales en TODOS los nombres para contar como texto
        // literal fijo -- si llegamos hasta aca es porque LargoPrefijoComun/LargoSufijoComun ya
        // lo garantiza para estos anchos, pero además ninguno de los dos puede "parecer" en si
        // mismo un valor de fecha valido: si lo pareciera, no hay forma de distinguir con la
        // evidencia actual si es texto fijo de verdad o un componente de fecha que se repite
        // (ej. el mismo año en todas las subcarpetas de mes de una sola carpeta de año todavia).
        var textoPrefijo = nombres[0][..prefijo];
        var textoSufijo = sufijo == 0 ? string.Empty : nombres[0][^sufijo..];
        return !PareceValorDeFecha(textoPrefijo) && !PareceValorDeFecha(textoSufijo);
    }

    private static bool PareceValorDeFecha(string texto) =>
        texto.Length > 0 && TodosLosTokens.Any(token => CoincideConPatron(texto, token));

    private static int LargoPrefijoComun(List<string> nombres)
    {
        if (nombres.Count == 1)
        {
            return 0;
        }

        var largoMinimo = nombres.Min(n => n.Length);
        var largo = 0;
        while (largo < largoMinimo && nombres.All(n => n[largo] == nombres[0][largo]))
        {
            largo++;
        }

        return largo;
    }

    private static int LargoSufijoComun(List<string> nombres)
    {
        if (nombres.Count == 1)
        {
            return 0;
        }

        var largoMinimo = nombres.Min(n => n.Length);
        var largo = 0;
        while (largo < largoMinimo && nombres.All(n => n[^(largo + 1)] == nombres[0][^(largo + 1)]))
        {
            largo++;
        }

        return largo;
    }

    /// <summary>
    /// Envuelve texto literal entre comillas simples para que DateTime.ToString/TryParseExact lo
    /// trate como texto fijo -- ej. una "M" o una "y" dentro del texto literal no debe
    /// interpretarse como parte del formato de fecha.
    /// </summary>
    private static string EnvolverLiteral(string texto) =>
        texto.Length == 0 ? string.Empty : "'" + texto.Replace("'", "\\'") + "'";

    public static bool CoincideConPatron(string? nombre, string patron) =>
        !string.IsNullOrEmpty(nombre) && DateTime.TryParseExact(nombre, patron, Cultura, DateTimeStyles.None, out _);

    /// <summary>
    /// Busca un ejemplo real de subcarpeta ya existente que coincide con el patron confirmado --
    /// para el preview obligatorio de "carpeta anterior" (Caso-1, punto 3): prueba visual de que
    /// el patron se entendio bien, no un calculo teorico.
    /// </summary>
    public static string? BuscarCarpetaAnteriorReal(string carpetaDestino, FormatoCarpeta formato, string? patron)
    {
        if (formato == FormatoCarpeta.Directo)
        {
            return Directory.Exists(carpetaDestino) ? carpetaDestino : null;
        }

        if (patron is null || !Directory.Exists(carpetaDestino))
        {
            return null;
        }

        var partes = patron.Split('\\');
        var nivel1 = Directory.GetDirectories(carpetaDestino)
            .Where(d => CoincideConPatron(Path.GetFileName(d), partes[0]))
            .OrderBy(d => Path.GetFileName(d), StringComparer.Ordinal)
            .ToList();

        if (nivel1.Count == 0)
        {
            return null;
        }

        if (partes.Length == 1)
        {
            return nivel1[^1];
        }

        foreach (var carpetaAnio in Enumerable.Reverse(nivel1))
        {
            var nivel2 = Directory.GetDirectories(carpetaAnio)
                .Where(d => CoincideConPatron(Path.GetFileName(d), partes[1]))
                .OrderBy(d => Path.GetFileName(d), StringComparer.Ordinal)
                .ToList();

            if (nivel2.Count > 0)
            {
                return nivel2[^1];
            }
        }

        return nivel1[^1];
    }

    /// <summary>Fecha del próximo período, para el preview de "carpeta futura" (Caso-1, punto 3).</summary>
    public static DateTime SiguientePeriodo(FormatoCarpeta formato, DateTime fecha) =>
        formato == FormatoCarpeta.AnioMes ? fecha.AddMonths(1) : fecha.AddYears(1);

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
