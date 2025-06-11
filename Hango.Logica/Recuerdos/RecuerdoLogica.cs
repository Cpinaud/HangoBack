using Hango.Datos.DTO.Recuerdo;
using Hango.Datos.DTO.Usuario;
using Hango.Datos.EF;
using Hango.Logica.Usuario;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Logica.Recuerdos
{
    public interface IRecuerdoLogica
    {
        Task<List<RecuerdoDTO>> ObtenerRecuerdosAsync(List<int>? idsGrupo, int idUsuario);
        Task<List<MatchesDTO>> ObtenerMatchesAsync(List<int>? idsGrupo, int idUsuario);
    }
    public class RecuerdoLogica : IRecuerdoLogica
    {
        private readonly HangoContext _context;
        private readonly IUsuarioLogica _usuarioLogica;

        public RecuerdoLogica(HangoContext context, IUsuarioLogica usuarioLogica)
        {
            _context = context;
            _usuarioLogica = usuarioLogica;
        }

        public async Task<List<MatchesDTO>> ObtenerMatchesAsync(List<int>? idsGrupo, int idUsuario)
        {
            var resultados = new List<MatchesDTO>();

            var disponibilidadUsuario = await _context.HorarioDisponible
                .Where(h => h.IdUsuario == idUsuario)
                .ToListAsync();

            if (idsGrupo == null || !idsGrupo.Any())
            {
                idsGrupo = await _context.Usuario_Grupo
                    .Where(ug => ug.IdUsuario == idUsuario)
                    .Select(ug => ug.IdGrupo)
                    .ToListAsync();
            }

            foreach (var grupoId in idsGrupo)
            {
                var grupo = await _context.Grupo.FindAsync(grupoId);
                var miembros = await _context.Usuario_Grupo
                    .Where(ug => ug.IdGrupo == grupoId && ug.IdUsuario != idUsuario)
                    .Select(ug => ug.IdUsuario)
                    .ToListAsync();

                foreach (var disponibilidad in disponibilidadUsuario)
                {
                    var usuariosCoinciden = new List<UsuarioDTO>();

                    foreach (var miembroId in miembros)
                    {
                        var dispMiembro = await _context.HorarioDisponible
                            .Where(d => d.IdUsuario == miembroId && d.DiaSemana == disponibilidad.DiaSemana)
                            .ToListAsync();

                        foreach (var d in dispMiembro)
                        {
                            if (IntervalosCoinciden(disponibilidad.HorarioInicio, disponibilidad.HorarioFin, d.HorarioInicio, d.HorarioFin))
                            {
                                var usuario = await _context.Usuario.FindAsync(miembroId);

                                usuariosCoinciden.Add(new UsuarioDTO
                                {
                                    Id = usuario.IdUsuario,
                                    Nombre = usuario.Nombre
                                });

                                break;
                            }
                        }
                    }

                    if (usuariosCoinciden.Any())
                    {
                        resultados.Add(new MatchesDTO
                        {
                            IdGrupo = grupo.IdGrupo,
                            NombreGrupo = grupo.Nombre,
                            DiaSemana = ((DayOfWeek)disponibilidad.DiaSemana).ToString(),
                            HoraDesde = disponibilidad.HorarioInicio,
                            HoraHasta = disponibilidad.HorarioFin,
                            UsuarioConMatch = usuariosCoinciden
                        });
                    }
                }
            }

            return resultados;
        }


        public async Task<List<RecuerdoDTO>> ObtenerRecuerdosAsync(List<int>? idsGrupo, int idUsuario)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Now);

            var recuerdos = await _context.Usuario_Plan
           .Where(up =>
               up.Estado == "INCLUIDO" &&
               up.UsuarioId == idUsuario &&
               up.Plan.Fecha < hoy &&
               (idsGrupo == null || !idsGrupo.Any() || idsGrupo.Contains(up.Plan.Propuesta.GrupoId))
           )
           .Select(up => new RecuerdoDTO
           {
               IdPlan = up.Plan.Id,
               Grupo = up.Plan.Propuesta.Grupo.Nombre,
               Fecha = up.Plan.Fecha,
               Lugar = up.Plan.Lugar,
               Descripcion = up.Plan.Descripcion
           })
           .ToListAsync();

            return recuerdos;
        }
        private bool IntervalosCoinciden(TimeSpan inicio1, TimeSpan fin1, TimeSpan inicio2, TimeSpan fin2)
        {
            return inicio1 < fin2 && inicio2 < fin1;
        }
    }
}
