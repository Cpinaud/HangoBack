using Hango.Logica.Preferencia;
using Microsoft.AspNetCore.Mvc;
namespace Hango.Controllers.Preferencia
{
    [ApiController]
    [Route("/Preferencia")]
    [ApiExplorerSettings(GroupName = "v1")]
    [Tags("5. Preferencia")]
    public class PreferenciaController : ControllerBase
    {

        private readonly IPreferenciaLogica _preferenciaLogica;

        public PreferenciaController(IPreferenciaLogica preferenciaLogica)
        {
            _preferenciaLogica = preferenciaLogica;
        }

        [HttpGet()]
        public async Task<IActionResult> ObtenerPreferencias()
        {
            var preferencias = await _preferenciaLogica.ObtenerPreferencias();
            return Ok(preferencias);
        }

        [HttpGet("usuario/{id}")]
        public async Task<IActionResult> ObtenerPreferenciasPorUsuario(int id)
        {
            var usuarioPreferencia = await _preferenciaLogica.ObtenerPreferenciasPorUsuario(id);
            return Ok(usuarioPreferencia);
        }

    }
}
