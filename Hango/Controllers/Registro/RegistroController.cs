using Hango.Datos.DTO.Preferencia;
using Hango.Datos.DTO.Registro;
using Hango.Datos.EF;

using Hango.Logica.Registro;
using Hango.Logica.Usuario;
using Hango.Logica.Utilidades;

using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;


namespace Hango.Controllers.Registro
{
    
    [ApiController]
    [Route("/Registro")]
    [ApiExplorerSettings(GroupName = "v1")]
    [Tags("3. Registro")]
    public class RegistroController : ControllerBase
    {

        private readonly IRegistroLogica _registroLogica;
        private readonly IUsuarioLogica _usuarioLogica;

        public RegistroController(IRegistroLogica registroLogica, IUsuarioLogica usuarioLogica)
        {
            _registroLogica = registroLogica;
            _usuarioLogica = usuarioLogica;
        }

        [HttpPost]
        public async Task<IActionResult> Registrar(RegistroUsuarioDTO usuario)
        {

            if (!ModelState.IsValid)
            {

                return BadRequest();
            }
            bool existeMail = await _registroLogica.ExisteMail(usuario.Email);
            if (existeMail)
            {
                return BadRequest("El email ya está registrado");
            }
            var nuevoUsuario = new Hango.Datos.EF.Usuario
            {
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Password_Hash = PasswordHash.Hash(usuario.Password),
                IdAvatar = usuario.IdAvatar ?? 1,
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now,
                CodigoVerificacion = Guid.NewGuid().ToString() //codigo unico de verificacion
            };
            
           
            var result = await _registroLogica.RegistrarUsuario(nuevoUsuario);



            return Ok(new { message = "Registro exitoso" });

        }

        [HttpGet("confirmar-cuenta")]
        public async Task<IActionResult> ConfirmarCuenta([FromQuery] string c)
        {
            var confirmado = await _registroLogica.ConfirmarCuenta(c);
           
            if (!confirmado)
                return NotFound("Código de verificación inválido.");

            return Ok("Cuenta confirmada correctamente.");
        }
        [HttpPost("guardar-datos")]
        public async Task<IActionResult> GuardarDatos([FromBody] DatosUsuarioRegistroDTO datos)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _usuarioLogica.GuardarDatosUsuario(datos);
            return Ok(new { mensaje = "Datos guardados correctamente" });
        }

      
    }
}
