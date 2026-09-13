using System.IO;

namespace Archivero.Datos;

public record ArchivoPendiente(int Id, string RutaArchivo, DateTime FechaDetectado)
{
    public string NombreArchivo => Path.GetFileName(RutaArchivo);
}

public class PendienteRepository
{
    public bool Agregar(string rutaArchivo)
    {
        using var conexion = BaseDeDatos.CrearConexion();
        using var comando = conexion.CreateCommand();
        comando.CommandText =
            """
            INSERT OR IGNORE INTO Pendientes (RutaArchivo, FechaDetectado)
            VALUES ($ruta, $fecha);
            """;
        comando.Parameters.AddWithValue("$ruta", rutaArchivo);
        comando.Parameters.AddWithValue("$fecha", DateTime.Now.ToString("O"));

        return comando.ExecuteNonQuery() > 0;
    }

    public bool Quitar(string rutaArchivo)
    {
        using var conexion = BaseDeDatos.CrearConexion();
        using var comando = conexion.CreateCommand();
        comando.CommandText = "DELETE FROM Pendientes WHERE RutaArchivo = $ruta;";
        comando.Parameters.AddWithValue("$ruta", rutaArchivo);
        return comando.ExecuteNonQuery() > 0;
    }

    public List<ArchivoPendiente> ObtenerTodos()
    {
        using var conexion = BaseDeDatos.CrearConexion();
        using var comando = conexion.CreateCommand();
        comando.CommandText = "SELECT Id, RutaArchivo, FechaDetectado FROM Pendientes ORDER BY FechaDetectado;";

        var resultado = new List<ArchivoPendiente>();
        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            resultado.Add(new ArchivoPendiente(
                lector.GetInt32(0),
                lector.GetString(1),
                DateTime.Parse(lector.GetString(2))));
        }

        return resultado;
    }
}
