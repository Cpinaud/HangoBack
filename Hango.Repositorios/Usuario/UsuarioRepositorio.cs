using Hango.Datos.EF;
using Hango.Repositorios.Utilidades;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Repositorios.Usuario
{
    public interface IUsuarioRepositorio
    {
        Task<Hango.Datos.EF.Usuario> FindUserByPassAndEmail(string email, string hashPassword);
        Task<Hango.Datos.EF.Usuario> FindUserByCodeVerificacion(string codigoVerificacion);
        Task<Hango.Datos.EF.Usuario> FindUserByEmail(string email);
        Task<Hango.Datos.EF.Usuario> FindUserById(int idUser);
     
        Task<bool> ConfirmarCuenta(string codigoVerificacion);
        Task<bool> ExisteMail(string email);
        Task<bool> AgregarUsuarioAsync(Hango.Datos.EF.Usuario usuario);
        Task<bool> ActualizarUsuarioAsync(Hango.Datos.EF.Usuario usuario);
        Task<Hango.Datos.EF.Usuario> ObtenerUsuarioConRelacionesAsync(int idUsuario);
        Task GuardarCambiosAsync();
    }
    public class UsuarioRepositorio : IUsuarioRepositorio
    {
        private readonly HangoContext _context;

        public UsuarioRepositorio(HangoContext context)
        {
            _context = context;
        }

        public async Task<bool> ActualizarUsuarioAsync(Datos.EF.Usuario usuario)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                _context.Usuario.Update(usuario);
                await _context.SaveChangesAsync();
                return true;
            });
        }

        public async Task<bool> AgregarUsuarioAsync(Datos.EF.Usuario usuario)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                await _context.Usuario.AddAsync(usuario);
                await _context.SaveChangesAsync();
                return true;
            });
        }

        public async Task<bool> ConfirmarCuenta(string codigoVerificacion)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var usuario = await FindUserByCodeVerificacion(codigoVerificacion);
                if (usuario == null)
                    return false;

                usuario.EstaVerificado = true;
                usuario.CodigoVerificacion = null;
                usuario.FechaVerificacion = DateTime.Now;
                usuario.UpdateAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        public async Task<bool> ExisteMail(string email)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(() =>
                _context.Usuario.AnyAsync(u => u.Email == email)
            );
        }

        public async Task<Datos.EF.Usuario> FindUserByCodeVerificacion(string codigoVerificacion)
        {
            var usuario = await RepositoryHelper.EjecutarConManejoErroresAsync(() =>
                _context.Usuario.FirstOrDefaultAsync(x => x.CodigoVerificacion == codigoVerificacion)
            );
            if (usuario == null)
                throw new Exception("Usuario no encontrado");
            return usuario;
        }

        public async Task<Datos.EF.Usuario> FindUserById(int idUser)
        {
            var usuario = await RepositoryHelper.EjecutarConManejoErroresAsync(() =>
                _context.Usuario.FirstOrDefaultAsync(x => x.IdUsuario == idUser)
            );
            if (usuario == null)
                throw new Exception("Usuario no encontrado");
            return usuario;
        }

        public async Task<Datos.EF.Usuario> FindUserByEmail(string email)
        {
            var usuario = await RepositoryHelper.EjecutarConManejoErroresAsync(() =>
                         _context.Usuario.FirstOrDefaultAsync(x => x.Email == email)
                            );

            if (usuario == null)
                throw new Exception("Usuario no encontrado");

            return usuario;
        }

        public async Task<Datos.EF.Usuario> FindUserByPassAndEmail(string email, string hashPassword)
        {
           var usuario = await RepositoryHelper.EjecutarConManejoErroresAsync(() =>
                _context.Usuario
                        .Where(x => x.Email == email && x.Password_Hash == hashPassword && x.EstaVerificado == true)
                        .FirstOrDefaultAsync()
            );
            if (usuario == null)
                throw new Exception("Usuario no encontrado");

            return usuario;
        }
        public async Task<Hango.Datos.EF.Usuario> ObtenerUsuarioConRelacionesAsync(int idUsuario)
        {
            var usuario = await _context.Usuario
                .Include(u => u.HorarioDisponible)
                .Include(u => u.Usuario_Preferencia)
                 .ThenInclude(up => up.IdPreferenciaNavigation)
                .Include(u => u.Presupuesto)
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

            if (usuario == null)
                throw new Exception("Usuario no encontrado");
            return usuario;
        }
        public async Task GuardarCambiosAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
