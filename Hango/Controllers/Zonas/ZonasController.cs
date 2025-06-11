using Hango.Logica.Grupos;
using Hango.Logica.Zona;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Hango.Controllers.Zonas
{

    [ApiController]
    [ApiExplorerSettings(GroupName = "v1")]
    [Tags("6. Zonas")]

    public class ZonasController : ControllerBase
    {
        private readonly IZonasLogica _zonasLogica;
        
        public ZonasController(IZonasLogica zonasLogica)
        {
            _zonasLogica = zonasLogica;
        }

        [HttpGet("/zonas")]
        [Authorize]
        public async Task<IActionResult> ObtenerZonas()
        {
            var usuario = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);
            try
            {

                var zonas = await _zonasLogica.ObtenerZonas();
                return Ok(zonas);
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("/zonas/{idZona}")]
        [Authorize]
        public async Task<IActionResult> ObtenerZonaPorId(int idZona)
        {
            var usuario = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);
            try
            {

                var zona = await _zonasLogica.ObtenerNombreZonaPorId(idZona);
                return Ok(zona);
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
