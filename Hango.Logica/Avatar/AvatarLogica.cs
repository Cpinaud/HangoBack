using Hango.Datos.EF;
using Hango.Repositorios.Avatar;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Hango.Logica.Avatar
{
    public interface IAvatarLogica
    {
        Task<List<Hango.Datos.EF.Avatar>> ObtenerAvatares();
        Task<Hango.Datos.EF.Avatar> ObtenerAvatarPorId(int idAvatar);
    }
    public class AvatarLogica : IAvatarLogica
    {
        private readonly IAvatarRepositorio _avatarRepositorio;

        public AvatarLogica(IAvatarRepositorio avatarRepositorio)
        {
            _avatarRepositorio = avatarRepositorio;
        }

        public async Task<List<Hango.Datos.EF.Avatar>> ObtenerAvatares()
        {
            var avatares = await _avatarRepositorio.ObtenerAvatares();
            return avatares;
        }

        public async Task<Hango.Datos.EF.Avatar> ObtenerAvatarPorId(int idAvatar)
        {
            var avatar = await _avatarRepositorio.ObtenerAvatarPorId(idAvatar);
            return avatar;
        }
    }
}
