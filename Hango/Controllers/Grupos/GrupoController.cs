using Hango.Datos.DTO.Grupo;
using Hango.Datos.EF;
using Hango.Dispatcher;
using Hango.Logica.Eventos;
using Hango.Logica.Grupos;
using Hango.Logica.Usuario;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using System.Text.Json;



namespace Hango.Controllers.Grupos
{
  
    [ApiController]
    [Route("/Grupos")]
    [ApiExplorerSettings(GroupName = "v1")]
    [Tags("1. Grupos")]

    public class GrupoController : ControllerBase
    {
        private readonly IGrupoLogica _grupoLogica;
        private readonly ISignalRDispatcher _signalRDispatcher;
        private readonly IUsuarioLogica _usuarioLogica;

        public GrupoController(IGrupoLogica grupoLogica, ISignalRDispatcher signalRDispatcher, IUsuarioLogica usuarioLogica )
        {
            _grupoLogica = grupoLogica;
            _signalRDispatcher = signalRDispatcher;
            _usuarioLogica = usuarioLogica;
        }

        //Para actualizar grupo
        [HttpPatch("actualizarGrupo/{idGrupo}")]
        [Authorize]

        public async Task<IActionResult> ActualizarGrupo(int idGrupo, [FromForm] GrupoUpdateDTO grupo)
        {
            try
            {
                var idUsuario = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);
                var grupoActualizado = await _grupoLogica.ActualizarGrupo(idGrupo,grupo,idUsuario);

                var mensaje = "Se editaron las preferencias del grupo.";
                string contenidoJson = JsonSerializer.Serialize(mensaje);
                await _signalRDispatcher.CrearYEmitirHub(idGrupo, "MENSAJE", contenidoJson, idUsuario);

                return Ok(grupoActualizado);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //Para acceder a la pantalla de unirse al grupo
        [HttpGet("invitacion/{token}")]

        public async Task<IActionResult> InvitacionAUnGrupo(string token)
        {
            var grupo = await _grupoLogica.ObtenerGrupoPorTokenInvitacion(token);

            if (grupo == null)
                return NotFound("Token inválido o grupo no encontrado");

            return Ok(grupo);
        }

        //Para unirse al grupo
        [HttpPost("unirseGrupo/{token}")]
        [Authorize]

        public async Task<IActionResult> UnirseAlGrupoMedianteLink(string token)
        {
            var grupo = await _grupoLogica.ObtenerGrupoPorTokenInvitacion(token);
            if (grupo == null)
                return NotFound("Token inválido o grupo no encontrado");
            var idUsuario = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);
            try {
                var idGrupo = grupo.IdGrupo;
                await _grupoLogica.UnirseAlGrupo(idUsuario, idGrupo);
                var usuario = await _usuarioLogica.ObtenerPerfilUsuarioById(idUsuario);

                var nombreUsuario = usuario.Nombre;
                var mensaje = $"{nombreUsuario} se ha unido al grupo.";
                string contenidoJson = JsonSerializer.Serialize(mensaje);
                await _signalRDispatcher.CrearYEmitirHub(idGrupo, "MENSAJE", contenidoJson, idUsuario);


                return Ok(grupo);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            

        }

        //Para regenerar link de invitacion
        [HttpGet("regenerarLink/{idGrupo}")]
        [Authorize]

        public async Task<IActionResult> RegenerarLinkInvitacion(int idGrupo)
        {
            var idUsuario = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);
            try
            {
                var token = await _grupoLogica.RegenerarTokenInvitacionGrupo(idGrupo, idUsuario);

                var mensaje = "Se regeneró el link de invitación al grupo.";
                string contenidoJson = JsonSerializer.Serialize(mensaje);
                await _signalRDispatcher.CrearYEmitirHub(idGrupo, "MENSAJE", contenidoJson, idUsuario);

                return Ok(token);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }



        //Para pantalla "Crear grupo"
        [HttpPost("crearGrupo")]
        [Authorize]

        public async Task<IActionResult> CrearGrupo([FromForm] GrupoInsertDTO grupo)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); 
            }
            var usuario = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);
            try
            {
                
                Grupo grupoCreado = await _grupoLogica.CrearGrupo(grupo, usuario);
                return Ok(grupoCreado.IdGrupo);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
           
        }



        //Para obtener un grupo para editar
        [HttpGet("editarGrupo/{idGrupo}")]
        [Authorize]
        public async Task<IActionResult> ObtenerGrupoPorIdParaEdicion(int idGrupo)
        {

            var idUsuario = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);
            try
            {
                var grupo = await _grupoLogica.ObtenerGrupoPorId(idGrupo, idUsuario);
                return Ok(grupo);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message); ;
            }

        }


        //Para obtener un grupo
        [HttpGet("{idGrupo}")]
        [Authorize]
        public async Task<IActionResult> ObtenerGrupoPorId(int idGrupo)
        {

            var idUsuario = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);
            try
            {
                var grupo = await _grupoLogica.ObtenerGrupoPorId(idGrupo, idUsuario);
                return Ok(grupo);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        //Para obtener integrantes de un grupo
        [HttpGet("integrantes/{idGrupo}")]
        [Authorize]
        public async Task<IActionResult> ObtenerIntegrantesDeGrupo(int idGrupo)
        {
            var idUsuario = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);
            try
            {
                var integrantes = await _grupoLogica.ObtenerIntegrantesGrupo(idGrupo,idUsuario);
                return Ok(integrantes);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        //Eliminar integrante (o abandonar grupo)
        [HttpDelete("{idGrupo}/eliminarIntegrante/{idUsuario}")]
        [Authorize]
        public async Task<IActionResult> EliminarIntregranteGrupo(int idGrupo, int idUsuario)
        {
            var idUsuarioEliminador = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);
            try
            {
                await _grupoLogica.EliminarIntegranteGrupo(idGrupo, idUsuario, idUsuarioEliminador);
                var mensaje = "";
                var contenidoJson = "";
                var usuario= await _usuarioLogica.ObtenerPerfilUsuarioById(idUsuario);

                var nombreUsuarioEliminado = usuario.Nombre;
                if (idUsuario == idUsuarioEliminador)
                    mensaje = $"{nombreUsuarioEliminado} abandonó el grupo.";
                else
                    mensaje = $"{nombreUsuarioEliminado} fue eliminado del grupo.";

                contenidoJson = JsonSerializer.Serialize(mensaje);
                await _signalRDispatcher.CrearYEmitirHub(idGrupo, "MENSAJE", contenidoJson, idUsuario);
                await _signalRDispatcher.NotificarActualizacionIntegrantes(idGrupo);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            
        }


        //Para pantalla "Mis grupos"
        [HttpGet("/misgrupos")]
        [Authorize]
        public async Task<IActionResult> ObtenerGruposDelUsuario()
        {
            var usuario = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);
            try
            {
                
                var grupos = await _grupoLogica.ObtenerGruposDelUsuario(usuario);
            return Ok(grupos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{idGrupo}/planesConfirmados")]
        [Authorize]
        public async Task<IActionResult> ObtenerPlanesConfirmadosDelGrupo(int idGrupo)
        {
            var idUsuario = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);
            try
            {
                var planes = await _grupoLogica.ObtenerPlanesConfirmadosDelGrupo(idGrupo,idUsuario);
                return Ok(planes);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("/ActivarGrupo/{idGrupo}")]
        [Authorize]

        public async Task<IActionResult> ActivarGrupo(int idGrupo)
        {
            var idUsuario = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);
            try
            {
                await _grupoLogica.ActivarGrupo(idGrupo,idUsuario);
                await _signalRDispatcher.NotificarActualizacionIntegrantes(idGrupo);
                return Ok("Grupo activado.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }




    }
}
