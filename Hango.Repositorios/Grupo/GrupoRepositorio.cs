using Hango.Datos.EF;
using Hango.Repositorios.Utilidades;
using Microsoft.EntityFrameworkCore;

namespace Hango.Repositorios.Grupo
{
    public interface IGrupoRepositorio
    {
        Task<bool> DatosUsuarioSeteados(int idUsuario);
        Task ActualizarGrupo(Datos.EF.Grupo grupo);
        Task<bool> UsuarioPerteneceAlGrupo(int idUsuario, int idGrupo);
        Task<bool> UsuarioEsAdminDeGrupo(int idUsuario, int idGrupo);
        Task<bool> GrupoConUnicoIntegrante(int idGrupo);
        Task<bool> GrupoConUnicoAdmin(int idGrupo);
        Task<Datos.EF.Grupo> ObtenerGrupoPorId(int idGrupo);
        Task<Datos.EF.Grupo> ObtenerGrupoPorToken(string token);
        Task<List<Usuario_Grupo>> ObtenerIntegrantesGrupo(int idGrupo);
        Task<List<Grupo_Preferencia>> ObtenerPreferenciasGrupo(int idGrupo);

        Task ActualizarPreferenciaGrupo(Grupo_Preferencia grupoPreferencia, int prioridad);
        Task<Usuario_Grupo> ObtenerIntegranteDeUnGrupoPorId(int idGrupo, int idUsuario);
        Task<List<int>> ObtenerIdPreferenciasGrupoPorTipo(int idGrupo, int estado);
        Task<List<Grupo_Preferencia>> ObtenerPreferenciasGrupoPorTipo(int idGrupo, int estado);
        Task<List<int>> ObtenerIdPreferenciasComunesOrdenadas(int idGrupo,List<int> idIntegrantes);
        Task<List<Planes>> ObtenerPlanesConfirmadosDelGrupo(int idGrupo);
        Task EliminarPreferenciasGrupoAuto(int idGrupo);
        Task EliminarIntegranteDelGrupo(Usuario_Grupo integrante);
        Task ActualizarToken(int idGrupo, string token);
        Task ReemplazarAdminGrupo(int idGrupo);
        Task<List<Datos.EF.Grupo>> ObtenerGruposDeUnUsuario(int idUsuario);
        Task GuardarGrupo(Datos.EF.Grupo grupo);
        Task GuardarPreferenciaGrupo(Datos.EF.Grupo_Preferencia grupo);

        Task GuardarUsuarioGrupo(Datos.EF.Usuario_Grupo grupo);

        Task VaciarZonasDelGrupo(Datos.EF.Grupo grupo);
        Task ActivarGrupo(int idGrupo);
    }

    public class GrupoRepositorio : IGrupoRepositorio
    {
        private readonly HangoContext _context;
        private readonly int valorPrioridadAuto = 0;
        private readonly int valorPrioridadAgregaManual = 1;
        private readonly int valorPrioridadEliminaManual = 2;

        public GrupoRepositorio(HangoContext context)
        {
            _context = context;

        }

        public async Task<bool> DatosUsuarioSeteados(int idUsuario)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var preferenciasUsuario = await _context.Usuario_Preferencia
                .Where(up => up.IdUsuario == idUsuario).CountAsync();
            var horariosUsuario = await _context.HorarioDisponible
                .Where(up => up.IdUsuario == idUsuario).CountAsync();
            if (preferenciasUsuario == 0 || horariosUsuario == 0)
                return false;

            return true;
            });
        }

        public async Task<Datos.EF.Grupo> ObtenerGrupoPorId(int idGrupo)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var grupo = await _context.Grupo
                        .Where(g => g.IdGrupo == idGrupo)
                        .Include(g => g.Zonas_Grupos)
                            .ThenInclude(zg => zg.IdZonaNavigation)
                        .Include(g => g.Usuario_Grupo)
                            .ThenInclude(ug => ug.IdUsuarioNavigation)
                        .Include(g => g.Grupo_Preferencia)
                            .ThenInclude(gp => gp.IdPreferenciaNavigation)
                            .FirstOrDefaultAsync();
                if (grupo == null)
                    throw new Exception("El grupo no existe.");
                return grupo;
            
            });
        }

        public async Task<List<int>> ObtenerIdPreferenciasComunesOrdenadas(int idGrupo,List<int> idIntegrantes)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var preferenciasGrupoEliminadas = await ObtenerIdPreferenciasGrupoPorTipo(idGrupo, valorPrioridadEliminaManual);
            var preferenciasGrupoAgregadas = await ObtenerIdPreferenciasGrupoPorTipo(idGrupo, valorPrioridadAgregaManual);
            var listaIdPreferencias = await _context.Usuario_Preferencia
                .Where(up => idIntegrantes.Contains(up.IdUsuario)
                && !preferenciasGrupoEliminadas.Contains(up.IdPreferencia)
                && !preferenciasGrupoAgregadas.Contains(up.IdPreferencia))
                .GroupBy(up => up.IdPreferencia)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .ToListAsync();

            return listaIdPreferencias;
            });
        }

        public async Task<List<int>> ObtenerIdPreferenciasGrupoPorTipo(int idGrupo, int estado)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var idPreferencias = await _context.Grupo_Preferencia
                .Where(gp => gp.IdGrupo == idGrupo && gp.Prioridad == estado)
                .Select(gp => gp.IdPreferencia)
                .ToListAsync();

            return idPreferencias;
        });
        }

        public async Task<List<Grupo_Preferencia>> ObtenerPreferenciasGrupoPorTipo(int idGrupo, int estado)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var preferencias = await _context.Grupo_Preferencia
                .Where(gp => gp.IdGrupo == idGrupo && gp.Prioridad == estado)
                .ToListAsync();

            return preferencias;
            });
        }

        public async Task<List<Usuario_Grupo>> ObtenerIntegrantesGrupo(int idGrupo)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var integrantes= await _context.Usuario_Grupo
                            .Include(ug => ug.IdUsuarioNavigation)
                            .Where(ug => ug.IdGrupo == idGrupo)
                            .ToListAsync();

            return integrantes;
        });
        }

        public async Task<bool> UsuarioPerteneceAlGrupo(int idUsuario, int idGrupo)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                return await _context.Usuario_Grupo
                .AnyAsync(ug => ug.IdUsuario == idUsuario && ug.IdGrupo == idGrupo);
            });
        }

        public async Task<bool> UsuarioEsAdminDeGrupo(int idUsuario, int idGrupo)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var esAdmin = await _context.Usuario_Grupo
                 .AnyAsync(ug => ug.IdUsuario == idUsuario && ug.IdGrupo == idGrupo && ug.Administrador == true);
            return esAdmin;
        });
        }

        public async Task<bool> GrupoConUnicoIntegrante(int idGrupo)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                return await _context.Usuario_Grupo
                   .Where(ug => ug.IdGrupo == idGrupo)
                   .CountAsync() == 1;
            });
        }

        public async Task<bool> GrupoConUnicoAdmin(int idGrupo)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                return await _context.Usuario_Grupo
               .Where(ug => ug.IdGrupo == idGrupo && ug.Administrador == true)
               .CountAsync() == 1;
            });
        }

        public async Task EliminarPreferenciasGrupoAuto(int idGrupo)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var preferenciasAuto = await ObtenerPreferenciasGrupoPorTipo(idGrupo, valorPrioridadAuto);
                _context.Grupo_Preferencia.RemoveRange(preferenciasAuto);
                await _context.SaveChangesAsync();
            });
        }

        public async Task<List<Datos.EF.Grupo>> ObtenerGruposDeUnUsuario(int idUsuario)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var idGruposDelUsuario = await _context.Usuario_Grupo
                        .Where(ug => ug.IdUsuario == idUsuario)
                        .Select(ug => ug.IdGrupo)
                        .Distinct()
                        .ToListAsync();

            var grupos = await _context.Grupo
                        .Where(g => idGruposDelUsuario.Contains(g.IdGrupo))
                        .Include(g => g.Zonas_Grupos)
                            .ThenInclude(zg => zg.IdZonaNavigation)
                        .Include(g => g.Usuario_Grupo)
                            .ThenInclude(ug => ug.IdUsuarioNavigation)
                        .ToListAsync();

            return grupos;
        });
        }

        public async Task<List<Planes>> ObtenerPlanesConfirmadosDelGrupo(int idGrupo)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                return await _context.Planes
                .Where(p => p.Propuesta.GrupoId == idGrupo && p.Estado == "1")
                .ToListAsync();
            });
        }

    
        public async Task GuardarPreferenciaGrupo(Grupo_Preferencia grupoPreferencia)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                _context.Grupo_Preferencia.Add(grupoPreferencia);
            await _context.SaveChangesAsync();
            });
        }

        public async Task GuardarGrupo(Datos.EF.Grupo grupo)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                _context.Grupo.Add(grupo);
                await _context.SaveChangesAsync();
            });
        }

        public async Task GuardarUsuarioGrupo(Usuario_Grupo UsuarioGrupo)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                _context.Usuario_Grupo.Add(UsuarioGrupo);
                await _context.SaveChangesAsync();
            });
        }

        public async Task<Datos.EF.Grupo> ObtenerGrupoPorToken(string token)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var grupo = await _context.Grupo
                        .Where(g => g.TokenInvitacion == token).FirstOrDefaultAsync();
                if (grupo == null)
                    throw new Exception("El grupo no existe o el token es inválido.");
                return grupo;
            });
        }

        public async Task<Usuario_Grupo> ObtenerIntegranteDeUnGrupoPorId(int idGrupo, int idUsuario)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var integrante= await _context.Usuario_Grupo
                 .Where(ug => ug.IdGrupo == idGrupo && ug.IdUsuario == idUsuario)
                 .FirstOrDefaultAsync();
                if (integrante == null)
                    throw new Exception("El integrante no pertenece al grupo especificado.");
                return integrante;
            });
        }

        public async Task ActualizarToken(int idGrupo,string token)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var grupo = await ObtenerGrupoPorId(idGrupo);
                grupo.TokenInvitacion = token;
                grupo.UpdateAt = DateTime.Now;
                await _context.SaveChangesAsync();
            });
        }

        public async Task ActualizarGrupo(Datos.EF.Grupo grupo)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                
               grupo.UpdateAt = DateTime.Now;
            await _context.SaveChangesAsync();
        });
        }

        public async Task ReemplazarAdminGrupo(int idGrupo)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var nuevoAdmin = await _context.Usuario_Grupo
                    .Where(ug => ug.IdGrupo == idGrupo && ug.Administrador == false)
                    .OrderBy(ug => ug.Ingreso)
                    .FirstOrDefaultAsync();
                if (nuevoAdmin == null)
                    throw new Exception("No hay integrantes disponibles para reemplazar al administrador.");
                nuevoAdmin.Administrador = true;
                await _context.SaveChangesAsync();
            });
        }

        public async Task EliminarIntegranteDelGrupo(Usuario_Grupo integrante)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                _context.Usuario_Grupo.Remove(integrante);
                await _context.SaveChangesAsync();
            });
        }

        public async Task<List<Grupo_Preferencia>> ObtenerPreferenciasGrupo(int idGrupo)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                return await _context.Grupo_Preferencia
            .Where(gp => gp.IdGrupo == idGrupo)
            .ToListAsync();
            });
        }

        public async Task ActualizarPreferenciaGrupo(Grupo_Preferencia grupoPreferencia, int prioridad)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                grupoPreferencia.Prioridad = prioridad;
            _context.Grupo_Preferencia.Update(grupoPreferencia);
            await _context.SaveChangesAsync();
            });
        }

        public async Task VaciarZonasDelGrupo(Datos.EF.Grupo grupo)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                _context.Zonas_Grupos.RemoveRange(grupo.Zonas_Grupos);
             grupo.Zonas_Grupos.Clear();
            await _context.SaveChangesAsync();
            });
        }

        public async Task ActivarGrupo(int idGrupo)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var grupo = await ObtenerGrupoPorId(idGrupo);
                if (grupo == null)
                    throw new Exception("El grupo no existe.");
                grupo.Activo = true;
                grupo.UpdateAt = DateTime.Now;
                await _context.SaveChangesAsync();
            });
        }

    }
}
