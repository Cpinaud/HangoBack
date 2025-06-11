using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;

namespace Hango.Repositorios.Utilidades
{
    public static class RepositoryHelper
    {
        public static async Task<T> EjecutarConManejoErroresAsync<T>(Func<Task<T>> funcion)
        {
            try
            {
                return await funcion();
            }
            catch (DbUpdateException ex)
            {
                throw new Exception("Error al actualizar los datos en la base de datos.", ex);
            }
            catch (SqlException ex)
            {
                throw new Exception("No se pudo acceder a la base de datos.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Ocurrió un error inesperado en el repositorio.", ex);
            }
        }

        public static async Task EjecutarConManejoErroresAsync(Func<Task> funcion)
        {
            try
            {
                await funcion();
            }
            catch (DbUpdateException ex)
            {
                throw new Exception("Error al actualizar los datos en la base de datos.", ex);
            }
            catch (SqlException ex)
            {
                throw new Exception("No se pudo acceder a la base de datos.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Ocurrió un error inesperado en el repositorio.", ex);
            }
        }

    }
}
