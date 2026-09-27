namespace Archivero.Servicios;

/// <summary>
/// Atajos de limpieza del nombre de archivo que el usuario revisa antes de guardar (Caso-4
/// punto 4, Caso-11 punto 6). Funciones puras: no validan seguridad (eso lo hace siempre
/// <see cref="ValidadorRutaService"/> al guardar), solo transforman el texto.
/// </summary>
public static class LimpiezaNombreService
{
    public static string DejarSoloNumeros(string nombre) => new(nombre.Where(char.IsDigit).ToArray());

    /// <summary>
    /// Quita los ceros de relleno a la izquierda de un nombre puramente numérico ("00123" → "123").
    /// Los ceros que son parte del valor no se tocan ("1200" queda igual), y un nombre que no es
    /// puramente numérico se devuelve tal cual. Un nombre hecho solo de ceros queda en "0", nunca vacío.
    /// </summary>
    public static string QuitarCerosIzquierda(string nombre)
    {
        if (nombre.Length == 0 || !nombre.All(char.IsAsciiDigit))
        {
            return nombre;
        }

        var sinCeros = nombre.TrimStart('0');
        return sinCeros.Length == 0 ? "0" : sinCeros;
    }
}
