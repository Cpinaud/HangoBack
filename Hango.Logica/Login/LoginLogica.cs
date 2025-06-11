using Hango.Datos.EF;
using Hango.Logica.Usuario;
using Hango.Repositorios.RefreshToken;
using Hango.Repositorios.Usuario;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Logica.Login
{
    public interface ILoginLogica
    {
        Task<Hango.Datos.EF.Usuario> ObtenerUserYContrasenia(string username, string hashPassword);
        Task GuardarRefreshToken(int idUsuario, string token, DateTime expiracion);
      //  Task<RefreshToken> ObtenerRefreshToken(string token);
        Task<bool> RevocarRefreshToken(string token);
    }
    public class LoginLogica : ILoginLogica
    {

        private readonly IRefreshTokenRepositorio _refreshTokenRepository;
        private readonly IUsuarioLogica _usuarioLogica;


        public LoginLogica(IRefreshTokenRepositorio refreshTokenRepository, IUsuarioLogica usuarioLogica)
        {
          
            _refreshTokenRepository = refreshTokenRepository;
            _usuarioLogica = usuarioLogica;
        }

        public async Task GuardarRefreshToken(int idUsuario, string token, DateTime expiracion)
        {
            var nuevoToken = new RefreshToken
            {
                IdUsuario = idUsuario,
                Token = token,
                HechoToken = DateTime.UtcNow,
                ExpiroToken = expiracion,
                RevocarToken = null

            };
            await _refreshTokenRepository.GuardarRefreshTokenAsync(nuevoToken);
        }

        

        public async Task<Hango.Datos.EF.Usuario> ObtenerUserYContrasenia(string mail, string hashPassword)
        {
            return await _usuarioLogica.FindUserByEmailAndPass(mail, hashPassword);
        }
        public async Task<bool> RevocarRefreshToken(string token)
        {
            return await _refreshTokenRepository.RevocarRefreshTokenAsync(token);
        }
        private async Task<RefreshToken> ObtenerRefreshToken(string token)
        {
            return await _refreshTokenRepository.FindRefreshToken(token);
        }
    }
}

