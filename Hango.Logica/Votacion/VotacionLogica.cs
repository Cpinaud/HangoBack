using Hango.Datos.DTO.Planes;
using Hango.Datos.DTO.Usuario;
using Hango.Datos.DTO.Votacion;
using Hango.Datos.EF;
using Hango.Repositorios.Avatar;
using Hango.Repositorios.Grupo;
using Hango.Repositorios.Votacion;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Plugins;
using System.Numerics;
using System.Text.Json;

namespace Hango.Logica.Votacion
{
    public interface IVotacionLogica
    {

        Task<VotacionPlanDTO> VotarPlan(VotacionPlanDTO votacion, int idUsuario);
        Task<PlanDTO>? DevuelvePlanSiYaSeConcreto(int idPropuesta);
        Task<bool> VerificarVotacionTerminada(int idPropuesta);

        Task CalcularPlanElegido(int idPropuesta);
        Task ParticiparEnVotacionPropuesta(int idGrupo, int idPropuesta, int idUsuario);
        Task<bool> NoParticiparEnVotacionPropuesta(int idGrupo, int idPropuesta, int idUsuario);
        Task<List<int>> ObtenerIdUsuariosPorPropuesta(int idPropuesta);

        Task<List<UsuarioDTO>> ObtenerUsuariosPorPropuesta(int idPropuesta);
    }

    public class VotacionLogica : IVotacionLogica
    {
        private readonly IVotacionRepositorio _votacionRepositorio;
        private readonly IGrupoRepositorio _grupoRepositorio; //necesario para verificar si el usuario pertenece al grupo
        private readonly HangoContext _context; //necesario para el manejo de transacciones (sólo se tiene que usar para eso en este archivo)

        public VotacionLogica(IVotacionRepositorio votacionRepositorio, HangoContext context, IGrupoRepositorio grupoRepositorio)
        {
            _votacionRepositorio = votacionRepositorio;
            _grupoRepositorio = grupoRepositorio;
            _context = context;
        }

        public async Task<VotacionPlanDTO> VotarPlan(VotacionPlanDTO votacion, int idUsuario)
        {
            var fechaVtoPropuesta = await _votacionRepositorio.ObtenerFechaVtoDePropuesta(votacion.IdPropuesta);
            if (fechaVtoPropuesta < DateTime.Now)
                throw new Exception("La votación ha vencido, no se puede votar en este plan.");

            var usuarioPerteneceAlGrupo = await _grupoRepositorio.UsuarioPerteneceAlGrupo(votacion.IdGrupo, idUsuario);

            var usuariosEnPropuesta = await _votacionRepositorio.ObtenerIdUsuariosPorPropuesta(votacion.IdPropuesta);
            if (!usuariosEnPropuesta.Contains(idUsuario))
                throw new Exception("El usuario no puede votar porque no pertenece a la propuesta");
            if(usuariosEnPropuesta.Count == 0)
                throw new Exception("No hay usuarios participando en la propuesta.");
            if (usuariosEnPropuesta.Count == 1)
                throw new Exception("Sos el único usuario en la votación, decile a tus amigos que se unan para que todos voten!.");
            if (votacion.Voto == null)
                throw new Exception("El voto no puede ser nulo.");


            var votoExistente = await _votacionRepositorio.UsuarioVotoPlan(idUsuario,votacion.IdPlan);
            if (!votoExistente)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _votacionRepositorio.RegistrarVoto(votacion, idUsuario);
                    if (await _votacionRepositorio.ValidarFinVotacionUsuario(votacion.IdPropuesta, idUsuario))
                        votacion.VotacionCompleta = true;
                    await transaction.CommitAsync();
                   
                    return votacion;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            else
            {
                throw new Exception("El usuario ya ha votado en este plan.");
            }

        }

        public async Task<bool> VerificarVotacionTerminada(int idPropuesta)
        {
           var usuariosDeLaPropuesta = await _votacionRepositorio.ObtenerIdUsuariosPorPropuesta(idPropuesta);
           var cantUsuariosDeLaPropuesta = usuariosDeLaPropuesta.Count;
            if (cantUsuariosDeLaPropuesta == 1)
                return false;
            var planesDeLaPropuesta = await _votacionRepositorio.ObtenerPlanesPorPropuesta(idPropuesta);

           var votosEnPropuesta = await _votacionRepositorio.ObtenerCantidadDeVotosPorPropuesta(planesDeLaPropuesta);
           var todosVotaron= votosEnPropuesta == (cantUsuariosDeLaPropuesta * planesDeLaPropuesta.Count);
           if (todosVotaron)
           {
               return true;
           }
           return false;
        }

        public async Task CalcularPlanElegido(int idPropuesta)
        {
            var planesDeLaPropuesta = await _votacionRepositorio.ObtenerPlanesPorPropuesta(idPropuesta);
            var registroDeVotos = await _votacionRepositorio.ObtenerRegistroDeVotosPorPropuesta(idPropuesta);
            var votosGroupOrdenados = CrearRegistroVotoDTO(registroDeVotos);
            var planIni = votosGroupOrdenados[0].IdPlan;
            bool todosLosPlanesPuntajeNegativo = true;
            bool ningunPlanCon50PorcientoMeGusta = true;
            bool todosTienenMasNoMeGustaQueMeGusta = true;
            // 0=Me da igual => Vale 1, 1=No me gusta  => Vale -2, 2=Me gusta  => Vale 2, 3= no participo  => NO CUENTA
            foreach (var grupo in votosGroupOrdenados.GroupBy(v => v.IdPlan))
            {
                int puntos = 0;
                int totalVotos = 0;
                int votosMeGusta = 0;
                int votosNoMeGusta = 0;
                foreach (var voto in grupo)
                {
                    switch (voto.Voto)
                    {
                        case 0: puntos += voto.Cantidad * 1; break;
                        case 1: puntos += voto.Cantidad * -2; votosNoMeGusta += voto.Cantidad;break;
                        case 2: puntos += voto.Cantidad * 2; votosMeGusta += voto.Cantidad; break;
                        case 3: break;
                    }
                    totalVotos += voto.Cantidad;
                    ningunPlanCon50PorcientoMeGusta = votosMeGusta >= (totalVotos / 2.0);
                    todosTienenMasNoMeGustaQueMeGusta = votosNoMeGusta > votosMeGusta;

                }
                foreach (var voto in grupo)
                {
                    voto.Puntos = puntos;
                    if (puntos > 0)
                        todosLosPlanesPuntajeNegativo = false;
                }
            }
            //TO DO: Sumar lógica de desempate si es necesario
            var votosOrdenados = votosGroupOrdenados.OrderByDescending(v => v.Puntos).ToList();

            if(todosLosPlanesPuntajeNegativo && ningunPlanCon50PorcientoMeGusta && todosTienenMasNoMeGustaQueMeGusta) {
                throw new Exception("Al grupo no le gustó ningun plan.");
                //TO DO: Habilitar un refresh de generacion de planes.
                //El endpoint no debe ser el mismo o al menos se debe validar que es un refresh y no una creacion inicial
                //para que permita realizarla ya que la creacion inicial valida la concretacion de algun plan en sugerencia anterior.
            }
            else {
                int idPlanElegido = votosOrdenados.FirstOrDefault().IdPlan;
                await _votacionRepositorio.MarcarPlanElegidoPorId(idPlanElegido);
            }
        }

        private List<RegistroVotoDTO> CrearRegistroVotoDTO(List<Voto_Plan> registroDeVotos)
        {
            try
            {
                return registroDeVotos
                    .GroupBy(v => new { v.PlanId, v.Voto })
                    .Select(g => new RegistroVotoDTO
                    {
                        IdPlan = g.Key.PlanId,
                        Voto = g.Key.Voto,
                        Cantidad = g.Count(),
                        Puntos = 0
                    })
                    .OrderBy(v => v.IdPlan)
                    .ToList();
            }
            catch (Exception)
            {
                throw new Exception("Error al crear registro de votos");
            }
        }

        public async Task<PlanDTO>? DevuelvePlanSiYaSeConcreto(int idPropuesta)
        {
            try
            {
                var planElegido = await _votacionRepositorio.ObtenerPlanElegidoPorIdPropuesta(idPropuesta);
                if (planElegido != null)
                {
                    var planDTO = new PlanDTO
                    {
                        PropuestaId = planElegido.PropuestaId,
                        Descripcion = planElegido.Descripcion,
                        Fecha = planElegido.Fecha,
                        Hora = planElegido.Hora,
                        Id = planElegido.Id,
                        PreferenciaId = planElegido.PreferenciaId,
                        DiaSemana = planElegido.DiaSemana,
                        Lugar = planElegido.Lugar,
                        Direccion = planElegido.Direccion,
                        Presupuesto = planElegido.Presupuesto,
                        UrlImagen = "preferencias/" + planElegido.PreferenciaId + ".png" 
                    };

                    return planDTO;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                throw new Exception("Error al obtener planes de la propuesta");
            }
        }

        public async Task ParticiparEnVotacionPropuesta(int idGrupo, int idPropuesta, int idUsuario)
        {
            var fechaVtoPropuesta = await _votacionRepositorio.ObtenerFechaVtoDePropuesta(idPropuesta);
            if (fechaVtoPropuesta < DateTime.Now)
                throw new Exception("La votación ha vencido, no se puede votar en esta propuesta.");
            var usuarioPertenece = await _grupoRepositorio.UsuarioPerteneceAlGrupo(idUsuario, idGrupo);
            if (!usuarioPertenece)
                throw new Exception("El usuario no pertenece al grupo");
            var usuariosEnPropuesta = await _votacionRepositorio.ObtenerIdUsuariosPorPropuesta(idPropuesta);
            if (usuariosEnPropuesta.Contains(idUsuario))
                throw new Exception("El usuario ya está participando en la propuesta");
            await _votacionRepositorio.AgregarUsuarioAPropuesta(idPropuesta, idUsuario);
        }

        public async Task<bool> NoParticiparEnVotacionPropuesta(int idGrupo, int idPropuesta, int idUsuario)
        {
            var fechaVtoPropuesta = await _votacionRepositorio.ObtenerFechaVtoDePropuesta(idPropuesta);
            if (fechaVtoPropuesta < DateTime.Now)
                throw new Exception("La votación ya cerró.");
            var usuarioPertenece = await _grupoRepositorio.UsuarioPerteneceAlGrupo(idUsuario, idGrupo);
            if (!usuarioPertenece)
                throw new Exception("El usuario no pertenece al grupo");
            var usuariosEnPropuesta = await _votacionRepositorio.ObtenerIdUsuariosPorPropuesta(idPropuesta);
            if (!usuariosEnPropuesta.Contains(idUsuario))
                throw new Exception("No puede salir de una propuesta a la que no pertenece");
            var anulaPropuesta = await _votacionRepositorio.ValidarSiReiniciaVotacion(idPropuesta, idUsuario);
            if (anulaPropuesta)
            {
                await _votacionRepositorio.EliminarVotacionesPropuesta(idPropuesta);
                return true;
            }
            else
            {
                var planes = await _votacionRepositorio.ObtenerPlanesPorPropuesta(idPropuesta);
                await _votacionRepositorio.EliminarVotosDelUsuario(planes, idUsuario);
                await _votacionRepositorio.EliminarUsuarioDeLaPropuesta(idUsuario, idPropuesta);
                return false;
            }
        }

        public async Task<List<int>> ObtenerIdUsuariosPorPropuesta(int idPropuesta)
        {
            var usuariosEnPropuesta = await _votacionRepositorio.ObtenerIdUsuariosPorPropuesta(idPropuesta);
            if (usuariosEnPropuesta == null || usuariosEnPropuesta.Count == 0)
                throw new Exception("No hay usuarios participando en la propuesta.");
            return usuariosEnPropuesta;
        }

        public async Task<List<UsuarioDTO>> ObtenerUsuariosPorPropuesta(int idPropuesta)
        {
            var usuarios = await _votacionRepositorio.ObtenerUsuariosPorPropuesta(idPropuesta);
            var usuariosDTO = usuarios.Select(u => new UsuarioDTO
            {
                Id = u.IdUsuario,
                Nombre = u.Nombre
            }).ToList();
            if (usuarios == null)
                throw new Exception("No hay usuarios participando en la propuesta.");
            
            return usuariosDTO;
        }
    }

}