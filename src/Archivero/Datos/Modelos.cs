namespace Archivero.Datos;

/// <summary>
/// Tipo de organización de subcarpetas dentro de la carpeta madre (Caso-3). El orden sigue la
/// lista completa del Paso 3b, por granularidad creciente. <see cref="Personalizado"/> es para
/// un patrón escrito a mano por el usuario ("Ninguna de estas — patrón personalizado").
/// </summary>
public enum FormatoCarpeta
{
    Directo,
    Anio,
    AnioSemestre,
    AnioTrimestre,
    AnioMes,
    AnioQuincena,
    AnioSemana,
    AnioMesDia,
    MesSinAnio,
    SemanaDelMes,
    Personalizado
}

public enum CampoMarca
{
    Emisor,
    Tipo,
    Fecha,
    NombreArchivo
}

/// <summary>Por qué un archivo terminó en "pendientes por reconocer" — determina qué pantalla abrir al revisarlo.</summary>
public enum MotivoPendiente
{
    /// <summary>No coincide con ninguna configuración: hace falta el asistente completo (REQ-003).</summary>
    NuevoDocumento,

    /// <summary>Coincidió con una configuración, pero un valor extraído no tiene forma válida.</summary>
    ValorInvalido,

    /// <summary>Coincidió con una configuración, pero ya existe un archivo con ese nombre en destino.</summary>
    Duplicado,

    /// <summary>Coincidió con una configuración, pero su carpeta de destino no está disponible ahora.</summary>
    CarpetaNoDisponible,

    /// <summary>Coincidió con una configuración, pero la carpeta del período actual todavía no existe (Caso-1, punto 2).</summary>
    PeriodoNuevo,

    /// <summary>El PDF no tiene texto extraíble: no se puede marcar por coordenadas (Caso-1, punto 1).</summary>
    SinTextoExtraible
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

    /// <summary>Caso-1, punto 5: si está activo, abre el archivo en el visor de PDF del sistema apenas se guarda solo.</summary>
    public bool AbrirDespuesDeGuardar { get; init; }
}
