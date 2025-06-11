
using Hango.Datos.DTO.Votacion;
using Hango.Datos.EF;
using Hango.Dispatcher;
using Hango.Logica.Eventos;
using Hango.Logica.Planes;
using Hango.Logica.Usuario;
using Hango.Logica.Votacion;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Hango.Controllers.Votacion
{

    [ApiController]
    [Route("/Votacion")]
    [ApiExplorerSettings(GroupName = "v1")]
    [Tags("1. Votacion")]
    public class VotacionController : ControllerBase
    {
        private readonly IVotacionLogica _votacionLogica;
        private readonly IUsuarioLogica _usuarioLogica;
        private readonly ISignalRDispatcher _signalRDispatcher;

        public VotacionController(IVotacionLogica votacionLogica,IUsuarioLogica usuarioLogica, ISignalRDispatcher signalRDispatcher)
        {
            _votacionLogica = votacionLogica;
            _usuarioLogica = usuarioLogica;
            _signalRDispatcher = signalRDispatcher;
        }

        [HttpPost("participar/{idGrupo}/{idPropuesta}")]
        [Authorize]
        public async Task<IActionResult> participarEnPropuesta(int idGrupo,int idPropuesta)
        {
            var idUsuario = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);
            if (idUsuario == 0)
                return Unauthorized("No se pudo obtener el ID del usuario.");

            try
            {
                await _votacionLogica.ParticiparEnVotacionPropuesta(idGrupo,idPropuesta,idUsuario);
                var usuario = await _usuarioLogica.ObtenerPerfilUsuarioById(idUsuario);
                var nombreUsuario = usuario.Nombre;
                var mensaje = $"{nombreUsuario} se unió a la votación.";
                string contenidoJson = JsonSerializer.Serialize(mensaje);
                await _signalRDispatcher.CrearYEmitirHub(idGrupo, "MENSAJE", contenidoJson, idUsuario);
                await _signalRDispatcher.NotificarActualizacionesVotacion(idGrupo);
                return Ok();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("noParticipar/{idGrupo}/{idPropuesta}")]
        [Authorize]
        public async Task<IActionResult> NoParticiparEnPropuesta(int idGrupo, int idPropuesta)
        {
            var idUsuario = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);
            if (idUsuario == 0)
                return Unauthorized("No se pudo obtener el ID del usuario.");

            try
            {
               var validaAnulacion = await _votacionLogica.NoParticiparEnVotacionPropuesta(idGrupo, idPropuesta, idUsuario);
                var usuario = await _usuarioLogica.ObtenerPerfilUsuarioById(idUsuario);
                var nombreUsuario = usuario.Nombre;
                var mensaje = $"{nombreUsuario} no quiere participar de la votación.";
                string contenidoJson = JsonSerializer.Serialize(mensaje);
                await _signalRDispatcher.CrearYEmitirHub(idGrupo, "MENSAJE", contenidoJson, idUsuario);
                await _signalRDispatcher.NotificarActualizacionesVotacion(idGrupo);


                if (validaAnulacion == true)
                {
                    mensaje = "Todos los integrantes del grupo se bajaron de la votación.";
                    contenidoJson = JsonSerializer.Serialize(mensaje);
                    await _signalRDispatcher.CrearYEmitirHub(idGrupo, "MENSAJE", contenidoJson, idUsuario);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("votarPlan")]
        [Authorize]
        public async Task<IActionResult> votar([FromBody] VotacionPlanDTO votacion)
        {
           var idUsuario = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);
            if (idUsuario == 0)
                return Unauthorized("No se pudo obtener el ID del usuario.");
            try
            {
                var votoPlan = await _votacionLogica.VotarPlan(votacion, idUsuario);
                var idGrupo = votacion.IdGrupo;
                var usuario = await _usuarioLogica.ObtenerPerfilUsuarioById(idUsuario);
                var nombreUsuario = usuario.Nombre;

                
                if (votoPlan.VotacionCompleta == true)
                {
                   
                    var mensaje = $"{nombreUsuario} ya votó.";
                    string contenidoJson = JsonSerializer.Serialize(mensaje);
                    await _signalRDispatcher.CrearYEmitirHub(idGrupo, "MENSAJE", contenidoJson, idUsuario);
                    await _signalRDispatcher.NotificarActualizacionesVotacion(idGrupo);
                }
                var votosFinalizados = await _votacionLogica.VerificarVotacionTerminada(votacion.IdPropuesta);
                if (votosFinalizados)
                {
                    await _votacionLogica.CalcularPlanElegido(votacion.IdPropuesta);
                }
                var planConcretado = await _votacionLogica.DevuelvePlanSiYaSeConcreto(votacion.IdPropuesta);
                if (planConcretado != null)
                {
                    try
                    {
                        //mensaje = $"El plan elegido para el {planConcretado.Fecha} es {planConcretado.Descripcion}.";
                        var contenidoPlanJson = JsonSerializer.Serialize(planConcretado);
                        await _signalRDispatcher.CrearYEmitirHub(idGrupo, "PLANCONCRETADO", contenidoPlanJson, idUsuario);
                        await _signalRDispatcher.NotificarActualizacionesVotacion(idGrupo);
                        //return Ok(new { votoPlan, planConcretado });
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(ex.Message);
                    }
                }
                return Ok(votoPlan);
//                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("usuariosPropuesta/{idPropuesta}")]
        [Authorize]
        public async Task<IActionResult> ObtenerUsuariosPorPropuesta(int idPropuesta)
        {
            try
            {
                var usuarios = await _votacionLogica.ObtenerUsuariosPorPropuesta(idPropuesta);
                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
