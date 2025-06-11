using Hango.Datos.DTO.Registro;
using Hango.Logica.Registro;
using Hango.Logica.Usuario;
using Microsoft.AspNetCore.Mvc;

namespace Hango.Controllers.Registro
{
    [ApiController]
    [Route("/Restablecer")]
    [ApiExplorerSettings(GroupName = "v1")]
    [Tags("7. Restablecer")]
    public class RestablecerController : ControllerBase
    {

        private readonly IUsuarioLogica _usuarioLogica;
        private readonly IRegistroLogica _registroLogica;

        public RestablecerController(IUsuarioLogica usuarioLogica, IRegistroLogica registroLogica)
        {
            _usuarioLogica = usuarioLogica;
            _registroLogica = registroLogica;
        }

        [HttpPost("solicitar-restablecimiento")]
        public async Task<IActionResult> SolicitarRestablecimiento([FromBody] string email)
        {
            var resultado = await _registroLogica.ObtenerMailYEnviarCodigoRestablecer(email);
            if (!resultado)
                return NotFound("No se encontró un usuario con ese correo electrónico.");

            return Ok("Se ha enviado un enlace para restablecer la contraseña.");
        }

        [HttpGet("solicitar-restablecimiento")]
        public async Task<IActionResult> MostrarFormularioRestablecimiento([FromQuery] string c)
        {

            var usuario = await _usuarioLogica.ObtenerUsuarioByCodigoDeVerficacion(c);
            var respuesta = new
            {
                usuario.Email,
                Codigo = usuario.CodigoVerificacion
            };

            return Ok(respuesta);
        }

        [HttpPost("restablecer")]
        public async Task<IActionResult> RestablecerPassword([FromBody] RestablecerDTO dto)
        {
            var resultado = await _registroLogica.RestablecerPass(dto.Codigo, dto.NuevaPassword);
            if (!resultado)
                return BadRequest("El código de verificación es inválido.");

            return Ok("Contraseña actualizada correctamente.");
        }
    }
}
