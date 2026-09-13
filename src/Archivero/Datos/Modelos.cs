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

public record ConfiguracionDocumento
{
    public int Id { get; init; }
    public required string Emisor { get; init; }
    public required string Tipo { get; init; }
    public required string CarpetaDestino { get; init; }
    public required FormatoCarpeta FormatoCarpeta { get; init; }
    public string? PatronCarpeta { get; init; }
    public required bool Renombrar { get; init; }
    public required List<Marca> Marcas { get; init; }
}
