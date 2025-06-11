using Hango.Datos.DTO.Usuario;
using Hango.Logica.Usuario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hango.Controllers.Usuario
{
    [ApiController]
    [Route("/Usuario")]
    [ApiExplorerSettings(GroupName = "v1")]
    [Tags("6. Usuario")]

    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioLogica _usuarioLogica;

        public UsuarioController(IUsuarioLogica usuarioLogica)
        {
            _usuarioLogica = usuarioLogica;
        }

        [HttpGet("ver-perfil")]
        [Authorize]
        public async Task<IActionResult> VerPerfil()
        {
            var idUsuario = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);

            if (idUsuario == 0)
                return Unauthorized("Usuario no autenticado.");

            var perfil = await _usuarioLogica.ObtenerPerfilUsuarioById(idUsuario);

            if (perfil == null)
                return NotFound("Usuario no encontrado.");

            return Ok(perfil);
        }

        [HttpPut("editar-perfil")]
        [Authorize]
        public async Task<IActionResult> EditarPerfil([FromBody] PerfilUsuarioDTO perfilDto)
        {
            var idUsuario = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);

            if (idUsuario == 0)
                return Unauthorized("Usuario no autenticado.");
            var resultado = await _usuarioLogica.EditarPerfilUsuario(perfilDto);

            if (!resultado)
                return NotFound("No se pudo actualizar el perfil.");

            return Ok("Perfil actualizado correctamente.");
        }

       
    }
}
