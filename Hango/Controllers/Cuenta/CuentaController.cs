using Hango.Datos.DTO.Cuenta;
using Hango.Datos.EF;
using Hango.Logica.Cuenta;
using Hango.Logica.MercadoPago;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hango.Controllers.Cuenta
{
    [ApiController]
    [Route("/Cuenta")]
    [ApiExplorerSettings(GroupName = "v1")]
    [Tags("8. Cuenta")]
    [Authorize]
    public class CuentaController : ControllerBase
    {

        private readonly ICuentaLogica _cuentaLogica;
        private readonly IMercadoPagoLogica _mercadoPagoLogica;

        public CuentaController(ICuentaLogica cuentaLogica, IMercadoPagoLogica mercadoPagoLogica)
        {
            _cuentaLogica = cuentaLogica;
            _mercadoPagoLogica = mercadoPagoLogica;
        }

    /*    [HttpPost]
        public async Task<IActionResult> CrearCuenta([FromForm] int IdPlan, [FromForm] int usuarioCreadorId, [FromForm] string descripcion,
                                                    [FromForm] decimal? montoInicial, [FromForm] decimal montoTotal)
        {
           int idCuenta = await _cuentaLogica.CrearCuentaNueva(IdPlan, usuarioCreadorId, descripcion, montoInicial, montoTotal);

            return Ok(new { IdCuenta = idCuenta, Mensaje = "Cuenta creada exitosamente" });
        } */
        [HttpPost("crear-partes-iguales")]
        public async Task<IActionResult> CrearCuentaPartesIguales([FromBody] CrearCuentaPartesIgualesRequest request)
        {
            try
            {
                var usuarioCreadorId = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);


                if (!await _cuentaLogica.TieneAutorizacion(usuarioCreadorId))
                {
                    var urlAutorizacion = _mercadoPagoLogica.GenerarLinkAutorizacionMercadoPago(usuarioCreadorId);
                    return BadRequest(new
                    {
                        mensaje = "Debes vincular tu cuenta de Mercado Pago para crear una cuenta.",
                        requiereAutorizacion = true,
                        tipo = "creador",
                        urlAutorizacion
                    });
                }
                if (usuarioCreadorId != 0 && !await _cuentaLogica.TieneAutorizacion(usuarioCreadorId))
                {
                    var urlAutorizacion = await _mercadoPagoLogica.GenerarLinkAutorizacionMercadoPago(usuarioCreadorId);
                    return BadRequest(new
                    {
                        mensaje = "El usuario receptor no tiene Mercado Pago autorizado.",
                        requiereAutorizacion = true,
                        tipo = "receptor",
                        usuarioId = usuarioCreadorId,
                        urlAutorizacion
                    });
                }
                //Que me puedan pagar con mercado pago 
                int idCuenta = await _cuentaLogica.CrearCuentaPartesIguales(
                    request.IdPlan,
                    usuarioCreadorId,
                    request.Descripcion,
                    request.MontoTotal,
                    usuarioCreadorId,
                    request.PaguenMercadoPago
                    
                );

                return Ok(new { IdCuenta = idCuenta });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpPost("crear-gastos-individuales")]
        public async Task<IActionResult> CrearCuentaGastosIndividuales([FromBody] CrearCuentaGastosIndividualesRequest request)
        {
            try
            {
                var usuarioCreadorId = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);
                int idCuenta = await _cuentaLogica.CrearCuentaGastosIndividuales(
                    request.IdPlan,
                    usuarioCreadorId,
                    request.Descripcion,
                    request.MontoTotal,
                    request.PaguenMercadoPago


                );

                return Ok(new { IdCuenta = idCuenta });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
        [HttpPost("cuenta/{idCuenta}/gastos-individuales")]
        public async Task<IActionResult> CrearGastosPorPersona(int idCuenta, [FromBody] List<GastoIndividualRequest> gastos)
        {
            try
            {
                await _cuentaLogica.AgregarGastosIndividuales(idCuenta, gastos);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpGet("{idCuenta}/participantes")]
        public async Task<IActionResult> ObtenerParticipantes(int idCuenta)
        {
            var participantes = await _cuentaLogica.ObtenerParticipantesDeUnaCuenta(idCuenta);

            if (participantes == null || !participantes.Any())
            {
                return NotFound(new { mensaje = "No se encontraron participantes para esta cuenta." });
            }

            return Ok(participantes);
        }

        [HttpPost("{idCuenta}/participacion")]
        public async Task<IActionResult> RegistrarParticipacion(int idCuenta, [FromBody] decimal? montoGastado, bool usarMercadoPago)
        {
            try
            {
                var usuarioId = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);
                if (usarMercadoPago)
                {
                    bool tieneAutorizacion = await _cuentaLogica.TieneAutorizacion(usuarioId);

                    if (!tieneAutorizacion)
                    {
                        var urlAutorizacion = await _mercadoPagoLogica.GenerarLinkAutorizacionMercadoPago(usuarioId);
                        return BadRequest(new
                        {
                            mensaje = "Debes autorizar tu cuenta de Mercado Pago antes de continuar.",
                            requiereAutorizacion = true,
                            urlAutorizacion
                        });
                    }
                }
                await _cuentaLogica.RegistrarParticipacion(idCuenta, montoGastado, usuarioId, usarMercadoPago);
                return Ok(new { mensaje = "Participación registrada correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = "Error al registrar la participación.", detalle = ex.Message });
            }
        }
        [HttpPost("{idCuenta}/cerrar")]
        public async Task<IActionResult> CerrarCuenta(int idCuenta)
        {
            try
            {
                var usuarioCreadorId = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);
                await _cuentaLogica.CerrarCuentaManualmente(idCuenta, usuarioCreadorId);
                return Ok(); 
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

     
        [HttpGet("{idCuenta}/resumen")]
        public async Task<ActionResult<ResumenCierreCuentaDTO>> ObtenerResumen(int idCuenta)
        {
            try
            {
                var resumen = await _cuentaLogica.ObtenerResumenDeCuenta(idCuenta);
                return Ok(resumen);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }



    }
}
