using Hango.Datos.DTO.Preferencia;
using Hango.Datos.EF;
using Hango.Repositorios.Preferencia;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Logica.Preferencia
{

    public interface IPreferenciaLogica
    {
        Task<List<Hango.Datos.EF.Preferencia>> ObtenerPreferencias();
        Task<int> ObtenerIdPreferenciaPorNombre(string nombrePref);
        Task<List<Hango.Datos.EF.Preferencia>> ObtenerPreferenciasPorUsuario(int id);
        Task<Usuario_Preferencia> GuardarPreferenciaDelUsuario(Usuario_Preferencia user);
        Task<List<Hango.Datos.EF.Preferencia>> ObtenerPreferenciasGrupoAsync(int idGrupo);
        Task ActualizarPreferencia(Hango.Datos.EF.Usuario usuario, List<PreferenciaDTO> preferencias);

    }
    public class PreferenciaLogica : IPreferenciaLogica
    {


        private readonly IPreferenciaRepositorio _preferenciaRepositorio;

        public PreferenciaLogica(IPreferenciaRepositorio preferenciaRepository)
        {
          
            _preferenciaRepositorio = preferenciaRepository;
        }

        public async Task<Usuario_Preferencia> GuardarPreferenciaDelUsuario(Usuario_Preferencia user)
        {
            var usuario = await _preferenciaRepositorio.GuardarPreferenciaDelUsuario(user);
            

            return usuario;
        }

        public async Task<List<Datos.EF.Preferencia>> ObtenerPreferencias()
        {
            return await _preferenciaRepositorio.ObtenerPreferencias();
        }

        public async Task<List<Datos.EF.Preferencia>> ObtenerPreferenciasPorUsuario(int id)
        {
            return await _preferenciaRepositorio.ObtenerPreferenciasPorUsuario(id);
        }

        public async Task<List<Hango.Datos.EF.Preferencia>> ObtenerPreferenciasGrupoAsync(int idGrupo)
        {
            return await _preferenciaRepositorio.ObtenerPreferenciasGrupoAsync(idGrupo);
        }

        public async Task<int> ObtenerIdPreferenciaPorNombre(string nombrePref)
        {
            return await _preferenciaRepositorio.ObtenerIdPreferenciaPorNombre(nombrePref);
        }

        public async Task ActualizarPreferencia(Datos.EF.Usuario usuario, List<PreferenciaDTO> preferencias)
        {
            await _preferenciaRepositorio.ActualizarPreferenciasAsync(usuario, preferencias);
        }
    }
}
