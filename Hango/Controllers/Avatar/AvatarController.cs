
using Microsoft.AspNetCore.Mvc;
using Hango.Datos.EF;
using Hango.Logica.Avatar;

namespace Hango.Controllers.Avatar
{
    [ApiController]
    [Route("/Avatar")]
    public class AvatarController : ControllerBase
    {

        private readonly IAvatarLogica _avatarLogica;
        public AvatarController(IAvatarLogica avatarLogica)
        {
            _avatarLogica = avatarLogica;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerAvatares()
        {
            try
            {
                var avatares = await _avatarLogica.ObtenerAvatares();
                return Ok(avatares);
            }
            catch
            {
                return StatusCode(500, "Ocurrió un error al obtener los avatares.");
            }
        }

        [HttpGet("{idAvatar}")]
        public async Task<IActionResult> ObtenerAvatarPorId(int idAvatar)
        {
            try
            {
                var avatar = await _avatarLogica.ObtenerAvatarPorId(idAvatar);
                if (avatar == null)
                {
                    return NotFound("Avatar no encontrado.");
                }
                return Ok(avatar);
            }
            catch
            {
                return StatusCode(500, "Ocurrió un error al obtener el avatar.");
            }
        }
    }
}
