using Microsoft.AspNetCore.Mvc;
using Hango.Logica.Notificaciones;
using Microsoft.AspNetCore.Authorization;

namespace Hango.Controllers.Notificaciones
{
    [ApiController]
    [Route("/Notificaciones")]

    public class NotificacionController : ControllerBase
    {
        private readonly INotificacionLogica _notificacionLogica;

        public NotificacionController(INotificacionLogica notificacionLogica)
        {
            _notificacionLogica = notificacionLogica;
        }

        [HttpPost("marcar-leido/{idNotificacion}")]
        [Authorize]
        public async Task<IActionResult> MarcarNotificacionComoLeida(int idNotificacion)
        {
            try
            {
                var usuarioId = UserLoginHelper.ObtenerIdUsuario(User);
                if (usuarioId == 0)
                {
                    return Unauthorized("Usuario no autorizado.");
                }
                var resultado = await _notificacionLogica.MarcarNotificacionComoLeida(idNotificacion, usuarioId);
                if (resultado)
                {
                    return Ok("Notificacion marcada como leída.");
                }
                else
                {
                    return NotFound("Notificacion no encontrada o ya leída.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
