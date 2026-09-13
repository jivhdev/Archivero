namespace Archivero.Datos;

public class ConfiguracionRepository
{
    public string? Obtener(string clave)
    {
        using var conexion = BaseDeDatos.CrearConexion();
        using var comando = conexion.CreateCommand();
        comando.CommandText = "SELECT Valor FROM Configuracion WHERE Clave = $clave";
        comando.Parameters.AddWithValue("$clave", clave);
        return comando.ExecuteScalar() as string;
    }

    public void Guardar(string clave, string valor)
    {
        using var conexion = BaseDeDatos.CrearConexion();
        using var comando = conexion.CreateCommand();
        comando.CommandText =
            """
            INSERT INTO Configuracion (Clave, Valor) VALUES ($clave, $valor)
            ON CONFLICT(Clave) DO UPDATE SET Valor = excluded.Valor;
            """;
        comando.Parameters.AddWithValue("$clave", clave);
        comando.Parameters.AddWithValue("$valor", valor);
        comando.ExecuteNonQuery();
    }
}
