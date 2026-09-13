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

/// <summary>
/// TextoReferencia es el texto que efectivamente se extrajo de esa coordenada en el momento
/// de crear la marca (para Emisor/Tipo). La coincidencia automática (REQ-002) compara contra
/// este valor, no contra el nombre de la entidad — porque el campo marcado no siempre es el
/// nombre visible (ej. puede ser un RUT o un código, si el nombre es un logo/imagen).
/// </summary>
public record Marca(CampoMarca Campo, int Pagina, double X, double Y, double Ancho, double Alto, string? TextoReferencia = null);

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
