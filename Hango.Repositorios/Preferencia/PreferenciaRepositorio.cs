using Hango.Datos.DTO.Preferencia;
using Hango.Datos.EF;
using Hango.Repositorios.Utilidades;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Repositorios.Preferencia
{
    public interface IPreferenciaRepositorio
    {
        Task<List<Hango.Datos.EF.Preferencia>> ObtenerPreferencias();
        Task<List<Datos.EF.Preferencia>> ObtenerPreferenciasGrupoAsync(int idGrupo);
        Task<List<Datos.EF.Preferencia>> ObtenerPreferenciasPorUsuario(int id);
        Task<int> ObtenerIdPreferenciaPorNombre(string nombrePref);
        Task ActualizarPreferenciasAsync(Hango.Datos.EF.Usuario usuario, List<PreferenciaDTO> preferencias);
        Task<Usuario_Preferencia> GuardarPreferenciaDelUsuario(Usuario_Preferencia user);
    }
    public class PreferenciaRepositorio : IPreferenciaRepositorio
    {
        private readonly HangoContext _context;

        public PreferenciaRepositorio(HangoContext context)
        {
            _context = context;
        }

        public async Task<List<Datos.EF.Preferencia>> ObtenerPreferencias()
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(() =>
                       _context.Preferencia.ToListAsync()
                       );
        }
        public async Task<List<Datos.EF.Preferencia>> ObtenerPreferenciasPorUsuario(int id)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(() =>
                (from up in _context.Usuario_Preferencia
                 join p in _context.Preferencia on up.IdPreferencia equals p.IdPreferencia
                 where up.IdUsuario == id
                 select p).ToListAsync()
            );
        }

        public async Task<List<Datos.EF.Preferencia>> ObtenerPreferenciasGrupoAsync(int idGrupo)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(() =>
                _context.Grupo_Preferencia
                    .Where(gp => gp.IdGrupo == idGrupo)
                    .Include(gp => gp.IdPreferenciaNavigation)
                    .Select(gp => gp.IdPreferenciaNavigation)
                    .ToListAsync()
            );
        }

        public async Task<int> ObtenerIdPreferenciaPorNombre(string nombrePref)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(() =>
                _context.Preferencia
                    .Where(p => p.Nombre == nombrePref)
                    .Select(p => p.IdPreferencia)
                    .FirstOrDefaultAsync()
            );
        }
        public async Task ActualizarPreferenciasAsync(Hango.Datos.EF.Usuario usuario, List<PreferenciaDTO> preferencias)
        {
            _context.Usuario_Preferencia.RemoveRange(usuario.Usuario_Preferencia);
            usuario.Usuario_Preferencia = preferencias.Select(p => new Usuario_Preferencia
            {
                IdPreferencia = p.IdPreferencia
            }).ToList();
        }
        public async Task<Usuario_Preferencia> GuardarPreferenciaDelUsuario(Usuario_Preferencia user)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                if (user == null)
                    throw new ArgumentNullException(nameof(user), "Las preferencias del usuario no pueden ser nulas.");

                await _context.Usuario_Preferencia.AddAsync(user);
            

                return user;
            });
        }
    }
}
