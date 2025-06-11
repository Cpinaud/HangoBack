using Hango.Datos.DTO.Votacion;
using Hango.Datos.EF;
using Hango.Repositorios.Utilidades;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Repositorios.Votacion
{
    public interface IVotacionRepositorio
    {

        Task<bool> UsuarioVotoPlan(int idUsuario, int idPlan);
        Task RegistrarVoto(VotacionPlanDTO votacion, int idUsuario);
        Task<List<int>> ObtenerPlanesPorPropuesta(int idPropuesta);
        Task<DateTime> ObtenerFechaVtoDePropuesta(int idPropuesta);
        Task<List<int>> ObtenerIdUsuariosPorPropuesta(int idPropuesta);
        Task<List<Datos.EF.Usuario>> ObtenerUsuariosPorPropuesta(int idPropuesta);
        Task<int> ObtenerCantidadDeVotosPorPropuesta(List<int> idPlanes);

        Task EliminarUsuarioDeLaPropuesta(int idUsuario, int idPropuesta);

        Task<List<Voto_Plan>> ObtenerRegistroDeVotosPorPropuesta(int idPropuesta);
        Task MarcarPlanElegidoPorId(int idPlan);
        Task<Planes?> ObtenerPlanElegidoPorIdPropuesta(int idPropuesta);
        Task AgregarUsuarioAPropuesta(int idPropuesta, int idUsuario);
        Task<bool> ValidarFinVotacionUsuario(int idPropuesta, int idUsuario);
        Task EliminarVotosDelUsuario(List<int> planes, int idUsuario);
        Task<bool> ValidarSiReiniciaVotacion(int idPropuesta, int idUsuario);
        Task EliminarVotacionesPropuesta(int idPropuesta);
    }

    public class VotacionRepositorio: IVotacionRepositorio
    {
        private readonly HangoContext _context;

        public VotacionRepositorio(HangoContext context)
        {
            _context = context;
        }

        public async Task<List<int>> ObtenerPlanesPorPropuesta(int idPropuesta)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                return await _context.Planes
                .Where(p => p.PropuestaId == idPropuesta)
                .Select(p => p.Id)
                .ToListAsync();
            });
        }

        public async Task RegistrarVoto(VotacionPlanDTO votacion, int idUsuario)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var nuevoVoto = new Voto_Plan
                {
                    UsuarioId = idUsuario,
                    PlanId = votacion.IdPlan,
                    Voto = votacion.Voto,
                    FechaVoto = DateTime.Now
                };
                _context.Voto_Plan.Add(nuevoVoto);
                await _context.SaveChangesAsync();
            });
        }
       

        public async Task<bool> UsuarioVotoPlan(int idUsuario, int idPlan)
        {
             return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                return await _context.Voto_Plan
                .AnyAsync(v => v.UsuarioId == idUsuario && v.PlanId == idPlan);
            });
        }

        public async Task<List<int>> ObtenerIdUsuariosPorPropuesta(int idPropuesta)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                return await _context.Usuario_Propuesta
                    .Where(up => up.IdPropuesta == idPropuesta)
                    .Select(up => up.IdUsuario)
                    .ToListAsync();
            });
        }
        public async Task<List<Datos.EF.Usuario>> ObtenerUsuariosPorPropuesta(int idPropuesta)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var usuariosPropuesta = ObtenerIdUsuariosPorPropuesta(idPropuesta);
                return await _context.Usuario
                    .Where(u => usuariosPropuesta.Result.Contains(u.IdUsuario))
                    .ToListAsync();
            });
        }


        public async Task<DateTime> ObtenerFechaVtoDePropuesta(int idPropuesta)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                return await _context.Propuesta
                     .Where(p => p.IdPropuesta == idPropuesta)
                    .Select(p => p.FechaVencimiento)
                    .FirstOrDefaultAsync();
            });
        }

        public async Task<int> ObtenerCantidadDeVotosPorPropuesta(List<int> idPlanes)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                return await _context.Voto_Plan
                   .Where(v => idPlanes.Contains(v.PlanId))
                   .CountAsync();
            });
        }
        public async Task EliminarUsuarioDeLaPropuesta(int idUsuario, int idPropuesta)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var usuarioPropuesta = await _context.Usuario_Propuesta
                    .FirstOrDefaultAsync(up => up.IdUsuario == idUsuario && up.IdPropuesta == idPropuesta);

                if (usuarioPropuesta != null)
                {
                    _context.Usuario_Propuesta.Remove(usuarioPropuesta);
                    await _context.SaveChangesAsync();
                }
            });
        }

        
        public async Task<List<Voto_Plan>> ObtenerRegistroDeVotosPorPropuesta(int idPropuesta)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                return await _context.Voto_Plan
                .Where(v => v.Plan.PropuestaId == idPropuesta)
                .ToListAsync();
            });
        }

        public async Task MarcarPlanElegidoPorId(int idPlan)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var plan= await _context.Planes
                        .Where(p => p.Id == idPlan)
                        .FirstOrDefaultAsync();
            plan.Estado = "1";

            _context.Planes.Update(plan);
            await _context.SaveChangesAsync();
            });
        }

        public async Task<Planes?> ObtenerPlanElegidoPorIdPropuesta(int idPropuesta)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                return await _context.Planes
                    .Where(p => p.PropuestaId == idPropuesta && p.Estado == "1")
                    .FirstOrDefaultAsync();
            });
        }

        public async Task AgregarUsuarioAPropuesta(int idPropuesta, int idUsuario)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var usuarioPropuesta = new Usuario_Propuesta
                {
                    IdUsuario = idUsuario,
                    IdPropuesta = idPropuesta
                };
                _context.Usuario_Propuesta.Add(usuarioPropuesta);
                await _context.SaveChangesAsync();
            });
        }



        public async Task<bool> ValidarFinVotacionUsuario(int idPropuesta, int idUsuario)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var cantPlanes = await ObtenerPlanesPorPropuesta(idPropuesta);
                var cantVotos = await _context.Voto_Plan
                    .Where(v => v.UsuarioId == idUsuario && v.Plan.PropuestaId == idPropuesta)
                    .CountAsync();
                if (cantPlanes == null)
                    throw new Exception("No hay planes asociados a esta propuesta.");
                if (cantVotos == 0)
                    return false;
                return cantPlanes.Count == cantVotos;
            });
        }

        public async Task EliminarVotosDelUsuario(List<int> planes, int idUsuario)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var votosAEliminar = await _context.Voto_Plan
                    .Where(v => planes.Contains(v.PlanId) && v.UsuarioId == idUsuario)
                    .ToListAsync();
                if (votosAEliminar.Any())
                {
                    _context.Voto_Plan.RemoveRange(votosAEliminar);
                    await _context.SaveChangesAsync();
                }
            });
        }

        public async Task<bool> ValidarSiReiniciaVotacion(int idPropuesta, int idUsuario)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var resultado = false;
                var count = await _context.Usuario_Propuesta
                    .Where(up => up.IdPropuesta == idPropuesta).CountAsync();
                if (count == 1)
                    resultado= true;
                return resultado;
            });
        }

        public async Task EliminarVotacionesPropuesta(int idPropuesta)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var votos = await _context.Voto_Plan
                    .Where(v => v.Plan.PropuestaId == idPropuesta).ToListAsync();
            _context.Voto_Plan.RemoveRange(votos);
            var usuariosPropuesta = await _context.Usuario_Propuesta
                    .Where(up => up.IdPropuesta == idPropuesta).ToListAsync();
            _context.Usuario_Propuesta.RemoveRange(usuariosPropuesta);
           
            

            await _context.SaveChangesAsync();

        });
        }
}
}
