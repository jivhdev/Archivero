namespace Archivero.Datos;

public enum FormatoCarpeta
{
    Directo,
    Anio,
    AnioMes
}

public enum CampoMarca
{
    Emisor,
    Tipo,
    Fecha,
    NombreArchivo
}

public record Marca(CampoMarca Campo, int Pagina, double X, double Y, double Ancho, double Alto);

/// <summary>
/// Un conjunto completo de marcas para UN diseño de documento. Una Configuracion puede tener
/// varios (cuando se vincula un documento con un diseño distinto a una configuracion existente).
/// </summary>
public record PatronReconocimiento(int Id, List<Marca> Marcas);

public record ConfiguracionDocumento
{
    public int Id { get; init; }
    public required string Emisor { get; init; }
    public required string Tipo { get; init; }
    public required string CarpetaDestino { get; init; }
    public required FormatoCarpeta FormatoCarpeta { get; init; }
    public string? PatronCarpeta { get; init; }
    public required bool Renombrar { get; init; }
    public required List<PatronReconocimiento> Patrones { get; init; }
}
