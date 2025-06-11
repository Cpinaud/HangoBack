using Hango.Datos.EF;
using Hango.Repositorios.Utilidades;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Repositorios.RefreshToken
{

    public interface IRefreshTokenRepositorio
    {
        Task GuardarRefreshTokenAsync(Hango.Datos.EF.RefreshToken token);
        Task<Hango.Datos.EF.RefreshToken> FindRefreshToken(string token);
        Task<bool> RevocarRefreshTokenAsync(string token);
    
    }
    public class RefreshTokenRepositorio : IRefreshTokenRepositorio
    {

        private readonly HangoContext _context;

        public RefreshTokenRepositorio(HangoContext context)
        {
            _context = context;
        }

        public async Task<Datos.EF.RefreshToken> FindRefreshToken(string token)
        {
            var refreshToken = await RepositoryHelper.EjecutarConManejoErroresAsync(() =>
                                _context.RefreshToken.FirstOrDefaultAsync(x => x.Token == token)
                                );
            if (refreshToken == null)
                throw new Exception("Refresh token no encontrado");
            return refreshToken;

        }

        public async Task GuardarRefreshTokenAsync(Datos.EF.RefreshToken token)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                await _context.RefreshToken.AddAsync(token);
                await _context.SaveChangesAsync();
            });
        }

        public async Task<bool> RevocarRefreshTokenAsync(string token)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var refreshToken = await FindRefreshToken(token);

                if (refreshToken == null || refreshToken.RevocarToken != null)
                    return false;

                refreshToken.RevocarToken = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            });
        }
    }
}
