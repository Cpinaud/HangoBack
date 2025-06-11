using Hango.Datos.EF;
using Hango.Repositorios.Utilidades;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Repositorios.Avatar
{
    public interface IAvatarRepositorio
    {
        Task<List<Datos.EF.Avatar>> ObtenerAvatares();
        Task<Datos.EF.Avatar> ObtenerAvatarPorId(int idAvatar);
    }
    public class AvatarRepositorio : IAvatarRepositorio
    {
        private readonly HangoContext _context;
        public AvatarRepositorio(HangoContext context)
        {
            _context = context;
        }

        public async Task<List<Datos.EF.Avatar>> ObtenerAvatares()
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var avatares =await _context.Avatar.ToListAsync();
            if (avatares == null)
                throw new Exception("No se encontraron avatares");
                return avatares;
            });
        }

        public async Task<Datos.EF.Avatar> ObtenerAvatarPorId(int idAvatar)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var avatar =await _context.Avatar.Where(av => av.IdAvatar == idAvatar).FirstOrDefaultAsync();
            if (avatar == null)
                throw new Exception("No se encontraron avatares");
            return avatar;
        });
        }
}
}
