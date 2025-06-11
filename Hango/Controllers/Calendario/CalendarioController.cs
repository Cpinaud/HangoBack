using Hango.Logica.Recuerdos;
using Hango.Logica.Registro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Hango.Controllers.Calendario
{
    [ApiController]
    [Route("/Calendario")]
    [ApiExplorerSettings(GroupName = "v1")]
    [Tags("7. Calendario")]

    public class CalendarioController : ControllerBase
    {
        private readonly IRecuerdoLogica _recuerdoLogica;

        public CalendarioController(IRecuerdoLogica recuerdoLogica)
        {
            _recuerdoLogica = recuerdoLogica;
        }

        [HttpGet("recuerdos")]
        [Authorize]
        public async Task<IActionResult> ObtenerRecuerdos([FromQuery] List<int> idGrupo)
        {
            
            
           var idUsuario = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);

            if (idUsuario == 0)
                return Unauthorized("Usuario no autenticado.");
            try
            {
                var recuerdos = await _recuerdoLogica.ObtenerRecuerdosAsync(idGrupo, idUsuario);
                return Ok(recuerdos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener recuerdos: {ex.Message}");
            }
        }
        [HttpGet("matches")]
        [Authorize]
        public async Task<IActionResult> ObtenerMatches([FromQuery] List<int> idsGrupo)
        {
            var idUsuario = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);

            if (idUsuario == 0)
                return Unauthorized("Usuario no autenticado.");
            try
            {
                var resultados = await _recuerdoLogica.ObtenerMatchesAsync(idsGrupo, idUsuario);
                return Ok(resultados);
            }
            catch (Exception ex)
            {
                
                return StatusCode(500, new { mensaje = "Error al obtener los matches", detalle = ex.Message });
            }
        
        }
    }
}
