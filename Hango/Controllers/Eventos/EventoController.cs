using Microsoft.AspNetCore.Mvc;
using Hango.Logica.Eventos;
using Microsoft.AspNetCore.Authorization;
using Hango.Logica.Grupos;
using Hango.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Hango.Controllers.Eventos
{
    [ApiController]
    [Route("/Eventos")]

    public class EventoController : ControllerBase
    {
        private readonly IEventoLogica _eventoLogica;
        private readonly IGrupoLogica _grupoLogica;
        private readonly IHubContext<GrupoHub> _hubContext;

        public EventoController(IEventoLogica eventoLogica, IGrupoLogica grupoLogica, IHubContext<GrupoHub> hubContext)
        {
            _eventoLogica = eventoLogica;
            _grupoLogica = grupoLogica;
            _hubContext = hubContext;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ObtenerEventosDelGrupo(int idGrupo)
        {
            try
            {
                var usuarioId = UserLoginHelper.ObtenerIdUsuario(User);
                if (usuarioId == 0) return Unauthorized("Usuario no autorizado.");

               var eventos = await _eventoLogica.ObtenerEventosPorGrupo(idGrupo,usuarioId);
                return Ok(eventos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

       
       

    }
}
