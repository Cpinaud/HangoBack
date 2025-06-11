using Hango.Logica.MercadoPago;
using Hango.Logica.Usuario;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hango.Controllers.Cuenta
{
    [ApiController]
    [Route("Pagos")]
    public class PagosController : Controller
    {

        private readonly IMercadoPagoLogica _mercadoPagoLogica;
        private readonly IUsuarioLogica _usuarioLogica;

        public PagosController(IMercadoPagoLogica mercadoPagoLogica, IUsuarioLogica usuarioLogica)
        {
            _mercadoPagoLogica = mercadoPagoLogica;
            _usuarioLogica = usuarioLogica;
        }
        [HttpGet("pago-exitoso")]
        public IActionResult PagoExitoso()
        {
            
            return Ok(new { estado = "exitoso", mensaje = "Pago exitoso. Gracias por tu compra." });
        }
        [HttpGet("pago-fallido")]
        public IActionResult PagoFallido()
        {
           
            return BadRequest(new { estado = "fallido", mensaje = "El pago falló. Intenta nuevamente." });
        }

        [HttpGet("pago-pendiente")]
        public IActionResult PagoPendiente()
        {
            
            return Ok(new { estado = "pendiente", mensaje = "Tu pago está pendiente de confirmación." });
        }
        [HttpGet("autorizar/{idUsuario}")]
        public async Task<IActionResult> AutorizarMercadoPago(int idUsuario)
        {
            var urlAutorizacion = await _mercadoPagoLogica.GenerarLinkAutorizacionMercadoPago(idUsuario);
            return Ok(urlAutorizacion);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> MercadoPagoCallback(string code, string state)
        {
            try
            {
                int idUsuario = int.Parse(state);
                var accessToken = await _mercadoPagoLogica.ObtenerAccessToken(code, idUsuario);
                await _usuarioLogica.ActualizarAccessTokenMercadoPago(idUsuario, accessToken);

                return Ok("Cuenta vinculada correctamente con Mercado Pago.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al vincular cuenta: {ex.Message}");
            }
        }
       
    }
}
