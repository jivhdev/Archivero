using System.IO;
using Microsoft.Data.Sqlite;

namespace Archivero.Datos;

public static class BaseDeDatos
{
    public static string RutaArchivo { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Archivero",
        "archivero.db");

    public static SqliteConnection CrearConexion()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(RutaArchivo)!);
        var conexion = new SqliteConnection($"Data Source={RutaArchivo}");
        conexion.Open();
        return conexion;
    }

    public static void AsegurarEsquema()
    {
        using var conexion = CrearConexion();
        using var comando = conexion.CreateCommand();
        comando.CommandText =
            """
            CREATE TABLE IF NOT EXISTS Configuracion (
                Clave TEXT PRIMARY KEY,
                Valor TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS EntidadesConocidas (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Categoria TEXT NOT NULL CHECK (Categoria IN ('Emisor', 'Tipo')),
                Nombre TEXT NOT NULL,
                UNIQUE (Categoria, Nombre)
            );

            CREATE TABLE IF NOT EXISTS Configuraciones (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EmisorId INTEGER NOT NULL REFERENCES EntidadesConocidas (Id),
                TipoId INTEGER NOT NULL REFERENCES EntidadesConocidas (Id),
                CarpetaDestino TEXT NOT NULL,
                FormatoCarpeta TEXT NOT NULL CHECK (FormatoCarpeta IN ('Directo', 'Anio', 'AnioMes')),
                PatronCarpeta TEXT NULL,
                Renombrar INTEGER NOT NULL DEFAULT 0,
                UNIQUE (EmisorId, TipoId)
            );

            CREATE TABLE IF NOT EXISTS PatronesReconocimiento (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ConfiguracionId INTEGER NOT NULL REFERENCES Configuraciones (Id)
            );

            CREATE TABLE IF NOT EXISTS Marcas (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PatronId INTEGER NOT NULL REFERENCES PatronesReconocimiento (Id),
                Campo TEXT NOT NULL CHECK (Campo IN ('Emisor', 'Tipo', 'Fecha', 'NombreArchivo')),
                Pagina INTEGER NOT NULL,
                X REAL NOT NULL,
                Y REAL NOT NULL,
                Ancho REAL NOT NULL,
                Alto REAL NOT NULL,
                TextoReferencia TEXT NULL,
                UNIQUE (PatronId, Campo)
            );

            CREATE TABLE IF NOT EXISTS Pendientes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RutaArchivo TEXT NOT NULL UNIQUE,
                FechaDetectado TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Borradores (
                RutaArchivo TEXT PRIMARY KEY,
                Datos TEXT NOT NULL
            );
            """;
        comando.ExecuteNonQuery();

        AgregarColumnaSiFalta(conexion, "Marcas", "TextoReferencia", "TEXT");
    }

    /// <summary>
    /// Migración mínima para bases ya existentes: agrega una columna nueva si todavía no está,
    /// sin tocar los datos ya guardados. SQLite no soporta "ADD COLUMN IF NOT EXISTS" directo.
    /// </summary>
    private static void AgregarColumnaSiFalta(SqliteConnection conexion, string tabla, string columna, string tipoSql)
    {
        using (var verificar = conexion.CreateCommand())
        {
            verificar.CommandText = $"PRAGMA table_info({tabla});";
            using var lector = verificar.ExecuteReader();
            while (lector.Read())
            {
                if (string.Equals(lector.GetString(1), columna, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
        }

        using var alterar = conexion.CreateCommand();
        alterar.CommandText = $"ALTER TABLE {tabla} ADD COLUMN {columna} {tipoSql} NULL;";
        alterar.ExecuteNonQuery();
    }
}
