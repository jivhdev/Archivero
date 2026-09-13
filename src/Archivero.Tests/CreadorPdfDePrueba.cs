using System.Text;

namespace Archivero.Tests;

internal static class CreadorPdfDePrueba
{
    /// <summary>
    /// Crea un PDF mínimo (una página, A4) con dos líneas de texto en posiciones bien
    /// separadas: una cerca del borde superior y otra cerca del borde inferior.
    /// </summary>
    public static string Crear(string carpetaDestino, string lineaSuperior, string lineaInferior)
    {
        var ruta = Path.Combine(carpetaDestino, $"prueba_{Guid.NewGuid():N}.pdf");

        var contenido =
            $"BT /F1 24 Tf 50 750 Td ({Escapar(lineaSuperior)}) Tj ET\n" +
            $"BT /F1 24 Tf 50 100 Td ({Escapar(lineaInferior)}) Tj ET\n";
        var contenidoBytes = Encoding.ASCII.GetBytes(contenido);

        using var stream = new MemoryStream();
        var offsets = new long[6];

        void Escribir(string texto) => stream.Write(Encoding.ASCII.GetBytes(texto));

        Escribir("%PDF-1.4\n");

        offsets[1] = stream.Position;
        Escribir("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        offsets[2] = stream.Position;
        Escribir("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

        offsets[3] = stream.Position;
        Escribir("3 0 obj\n<< /Type /Page /Parent 2 0 R /Resources << /Font << /F1 4 0 R >> >> /MediaBox [0 0 595 842] /Contents 5 0 R >>\nendobj\n");

        offsets[4] = stream.Position;
        Escribir("4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");

        offsets[5] = stream.Position;
        Escribir($"5 0 obj\n<< /Length {contenidoBytes.Length} >>\nstream\n");
        stream.Write(contenidoBytes, 0, contenidoBytes.Length);
        Escribir("\nendstream\nendobj\n");

        var xrefOffset = stream.Position;
        Escribir("xref\n0 6\n0000000000 65535 f \n");
        for (var i = 1; i <= 5; i++)
        {
            Escribir($"{offsets[i]:D10} 00000 n \n");
        }

        Escribir($"trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");

        File.WriteAllBytes(ruta, stream.ToArray());
        return ruta;
    }

    private static string Escapar(string texto) =>
        texto.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
