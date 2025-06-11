using Hango.Logica.Login;
using Hango.Logica.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Hango.Controllers.Login
{
   
    [ApiController]
    [Route("/Login")]
    [ApiExplorerSettings(GroupName = "v1")]
    [Tags("2. Login")]
    public class LoginController : ControllerBase
    {

        private readonly ILoginLogica _loginLogica;
        private readonly IConfiguration _configuration;

        public LoginController(ILoginLogica loginLogica, IConfiguration configuration)
        {
            _loginLogica = loginLogica;
            _configuration = configuration;
        }


        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _loginLogica.ObtenerUserYContrasenia(request.Email, PasswordHash.Hash(request.Password));

            if (user == null)
            {
                return Unauthorized(new { message = "Usuario o contraseña incorrectos" });
            }

            var claims = new[]
            {
            new Claim(ClaimTypes.Name, user.Email),
            //CIN: Lo agregue para usarlo en grupos hasta que esté el refreshToken
            new Claim(ClaimTypes.NameIdentifier, user.IdUsuario.ToString())
            };

          
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            await _loginLogica.GuardarRefreshToken(user.IdUsuario, tokenString, token.ValidTo);

            return Ok(new
            {
                token = tokenString,
                usuario = user.Email
            });
        }
       [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
                return BadRequest(new { message = "Token no proporcionado." });

            var exito = await _loginLogica.RevocarRefreshToken(token);

            if (!exito)
                return BadRequest(new { message = "Token inválido" });

            return Ok(new { message = "Sesion cerrada correctamente." });
        } 
    }
}
