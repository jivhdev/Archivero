using Microsoft.Data.Sqlite;

namespace Archivero.Datos;

public class ConfiguracionDocumentoRepository
{
    private readonly EntidadRepository _entidades = new();

    public bool ExisteCoincidenciaExacta(string emisor, string tipo) =>
        BuscarPorEmisorYTipo(emisor, tipo) is not null;

    public ConfiguracionDocumento? BuscarPorEmisorYTipo(string emisor, string tipo)
    {
        using var conexion = BaseDeDatos.CrearConexion();
        using var comando = conexion.CreateCommand();
        comando.CommandText =
            """
            SELECT c.Id, ce.Nombre, ct.Nombre, c.CarpetaDestino, c.FormatoCarpeta, c.PatronCarpeta, c.Renombrar
            FROM Configuraciones c
            JOIN EntidadesConocidas ce ON ce.Id = c.EmisorId
            JOIN EntidadesConocidas ct ON ct.Id = c.TipoId
            WHERE ce.Nombre = $emisor AND ct.Nombre = $tipo;
            """;
        comando.Parameters.AddWithValue("$emisor", emisor);
        comando.Parameters.AddWithValue("$tipo", tipo);

        using var lector = comando.ExecuteReader();
        if (!lector.Read())
        {
            return null;
        }

        var configuracionId = lector.GetInt32(0);
        var configuracion = LeerConfiguracion(lector);
        lector.Close();

        return configuracion with { Patrones = ObtenerPatrones(conexion, configuracionId) };
    }

    public List<ConfiguracionDocumento> ObtenerTodasConPatrones()
    {
        using var conexion = BaseDeDatos.CrearConexion();
        using var comando = conexion.CreateCommand();
        comando.CommandText =
            """
            SELECT c.Id, ce.Nombre, ct.Nombre, c.CarpetaDestino, c.FormatoCarpeta, c.PatronCarpeta, c.Renombrar
            FROM Configuraciones c
            JOIN EntidadesConocidas ce ON ce.Id = c.EmisorId
            JOIN EntidadesConocidas ct ON ct.Id = c.TipoId;
            """;

        var configuraciones = new List<ConfiguracionDocumento>();
        using (var lector = comando.ExecuteReader())
        {
            while (lector.Read())
            {
                configuraciones.Add(LeerConfiguracion(lector));
            }
        }

        return configuraciones
            .Select(c => c with { Patrones = ObtenerPatrones(conexion, c.Id) })
            .ToList();
    }

    public int GuardarNueva(
        string emisor,
        string tipo,
        string carpetaDestino,
        FormatoCarpeta formatoCarpeta,
        string? patronCarpeta,
        bool renombrar,
        List<Marca> marcas)
    {
        var emisorId = _entidades.ObtenerOCrear(CategoriaEntidad.Emisor, emisor);
        var tipoId = _entidades.ObtenerOCrear(CategoriaEntidad.Tipo, tipo);

        using var conexion = BaseDeDatos.CrearConexion();
        using var transaccion = conexion.BeginTransaction();

        int configuracionId;
        using (var insertarConfig = conexion.CreateCommand())
        {
            insertarConfig.Transaction = transaccion;
            insertarConfig.CommandText =
                """
                INSERT INTO Configuraciones (EmisorId, TipoId, CarpetaDestino, FormatoCarpeta, PatronCarpeta, Renombrar)
                VALUES ($emisorId, $tipoId, $carpetaDestino, $formato, $patron, $renombrar);
                SELECT last_insert_rowid();
                """;
            insertarConfig.Parameters.AddWithValue("$emisorId", emisorId);
            insertarConfig.Parameters.AddWithValue("$tipoId", tipoId);
            insertarConfig.Parameters.AddWithValue("$carpetaDestino", carpetaDestino);
            insertarConfig.Parameters.AddWithValue("$formato", formatoCarpeta.ToString());
            insertarConfig.Parameters.AddWithValue("$patron", (object?)patronCarpeta ?? DBNull.Value);
            insertarConfig.Parameters.AddWithValue("$renombrar", renombrar ? 1 : 0);

            configuracionId = (int)(long)insertarConfig.ExecuteScalar()!;
        }

        AgregarPatron(conexion, transaccion, configuracionId, marcas);

        transaccion.Commit();
        return configuracionId;
    }

    public void AgregarPatronAConfiguracionExistente(int configuracionId, List<Marca> marcas)
    {
        using var conexion = BaseDeDatos.CrearConexion();
        using var transaccion = conexion.BeginTransaction();
        AgregarPatron(conexion, transaccion, configuracionId, marcas);
        transaccion.Commit();
    }

    private static void AgregarPatron(SqliteConnection conexion, SqliteTransaction transaccion, int configuracionId, List<Marca> marcas)
    {
        int patronId;
        using (var insertarPatron = conexion.CreateCommand())
        {
            insertarPatron.Transaction = transaccion;
            insertarPatron.CommandText =
                """
                INSERT INTO PatronesReconocimiento (ConfiguracionId) VALUES ($configuracionId);
                SELECT last_insert_rowid();
                """;
            insertarPatron.Parameters.AddWithValue("$configuracionId", configuracionId);
            patronId = (int)(long)insertarPatron.ExecuteScalar()!;
        }

        foreach (var marca in marcas)
        {
            using var insertarMarca = conexion.CreateCommand();
            insertarMarca.Transaction = transaccion;
            insertarMarca.CommandText =
                """
                INSERT INTO Marcas (PatronId, Campo, Pagina, X, Y, Ancho, Alto, TextoReferencia)
                VALUES ($patronId, $campo, $pagina, $x, $y, $ancho, $alto, $textoReferencia);
                """;
            insertarMarca.Parameters.AddWithValue("$patronId", patronId);
            insertarMarca.Parameters.AddWithValue("$campo", marca.Campo.ToString());
            insertarMarca.Parameters.AddWithValue("$pagina", marca.Pagina);
            insertarMarca.Parameters.AddWithValue("$x", marca.X);
            insertarMarca.Parameters.AddWithValue("$y", marca.Y);
            insertarMarca.Parameters.AddWithValue("$ancho", marca.Ancho);
            insertarMarca.Parameters.AddWithValue("$alto", marca.Alto);
            insertarMarca.Parameters.AddWithValue("$textoReferencia", (object?)marca.TextoReferencia ?? DBNull.Value);
            insertarMarca.ExecuteNonQuery();
        }
    }

    public List<string> ObtenerPatronesDeCarpetaConocidos()
    {
        using var conexion = BaseDeDatos.CrearConexion();
        using var comando = conexion.CreateCommand();
        comando.CommandText = "SELECT DISTINCT PatronCarpeta FROM Configuraciones WHERE PatronCarpeta IS NOT NULL;";

        var resultado = new List<string>();
        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            resultado.Add(lector.GetString(0));
        }

        return resultado;
    }

    private static ConfiguracionDocumento LeerConfiguracion(SqliteDataReader lector) => new()
    {
        Id = lector.GetInt32(0),
        Emisor = lector.GetString(1),
        Tipo = lector.GetString(2),
        CarpetaDestino = lector.GetString(3),
        FormatoCarpeta = Enum.Parse<FormatoCarpeta>(lector.GetString(4)),
        PatronCarpeta = lector.IsDBNull(5) ? null : lector.GetString(5),
        Renombrar = lector.GetInt32(6) != 0,
        Patrones = []
    };

    private static List<PatronReconocimiento> ObtenerPatrones(SqliteConnection conexion, int configuracionId)
    {
        using var comando = conexion.CreateCommand();
        comando.CommandText =
            """
            SELECT m.PatronId, m.Campo, m.Pagina, m.X, m.Y, m.Ancho, m.Alto, m.TextoReferencia
            FROM Marcas m
            JOIN PatronesReconocimiento p ON p.Id = m.PatronId
            WHERE p.ConfiguracionId = $configuracionId
            ORDER BY m.PatronId;
            """;
        comando.Parameters.AddWithValue("$configuracionId", configuracionId);

        var patrones = new Dictionary<int, List<Marca>>();
        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            var patronId = lector.GetInt32(0);
            if (!patrones.TryGetValue(patronId, out var marcas))
            {
                marcas = [];
                patrones[patronId] = marcas;
            }

            marcas.Add(new Marca(
                Enum.Parse<CampoMarca>(lector.GetString(1)),
                lector.GetInt32(2),
                lector.GetDouble(3),
                lector.GetDouble(4),
                lector.GetDouble(5),
                lector.GetDouble(6),
                lector.IsDBNull(7) ? null : lector.GetString(7)));
        }

        return patrones.Select(p => new PatronReconocimiento(p.Key, p.Value)).ToList();
    }
}
