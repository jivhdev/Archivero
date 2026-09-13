using System.IO;
using Microsoft.Data.Sqlite;

namespace Archivero.Datos;

public static class BaseDeDatos
{
    public static string RutaArchivo { get; } = Path.Combine(
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
            """;
        comando.ExecuteNonQuery();
    }
}
