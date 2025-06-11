using Hango.Datos.EF;
using Hango.Logica.Utilidades;
using Hango.Repositorios.Usuario;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Logica.Registro
{
    public interface IRegistroLogica
    {
        Task<bool> ExisteMail(string email);
        Task<bool> RegistrarUsuario(Hango.Datos.EF.Usuario nuevoUsuario);

        Task<bool> ObtenerMailYEnviarCodigoRestablecer(string email);
        Task<bool> RestablecerPass(string codigoVerifacion, string nuevaPassword);
        Task<bool> ConfirmarCuenta(string codigoVerificacion);
    }
    public class RegistroLogica : IRegistroLogica
    {

        
        private readonly ICorreoLogica _correoLogica;
        private readonly IUsuarioRepositorio _usuarioRepository;

        public RegistroLogica(ICorreoLogica correoLogica, IUsuarioRepositorio usuarioRepository)
        {
            
            _correoLogica = correoLogica;
            _usuarioRepository = usuarioRepository;
        }

        public async Task<bool> ConfirmarCuenta(string codigoVerificacion)
        {
            return await _usuarioRepository.ConfirmarCuenta(codigoVerificacion);
        }

        public async Task<bool> ExisteMail(string email)
        {
            return await _usuarioRepository.ExisteMail(email);
        }

        public async Task<bool> ObtenerMailYEnviarCodigoRestablecer(string email)
        {
            var usuario = await _usuarioRepository.FindUserByEmail(email);
            if (usuario == null) return false;
            usuario.CodigoVerificacion = Guid.NewGuid().ToString();
            usuario.UpdateAt = DateTime.Now;
            var actualizado = await _usuarioRepository.ActualizarUsuarioAsync(usuario);
            if (!actualizado) return false;
            await _correoLogica.EnviarCodigo(usuario.Email, usuario.CodigoVerificacion, true);

            return true;
        }

        public async Task<bool> RegistrarUsuario(Hango.Datos.EF.Usuario nuevoUsuario)
        {
           
            if (await _usuarioRepository.ExisteMail(nuevoUsuario.Email))
                return false;

            if (string.IsNullOrWhiteSpace(nuevoUsuario.Nombre) ||
                string.IsNullOrWhiteSpace(nuevoUsuario.Email) ||
                string.IsNullOrWhiteSpace(nuevoUsuario.Password_Hash) || (nuevoUsuario.IdAvatar == 0))
            {
                return false;
            }

            nuevoUsuario.CreateAt = DateTime.Now;
            nuevoUsuario.UpdateAt = DateTime.Now;

            var resultado = await _usuarioRepository.AgregarUsuarioAsync(nuevoUsuario);
            if (!resultado) return false;

            await _correoLogica.EnviarCodigo(nuevoUsuario.Email, nuevoUsuario.CodigoVerificacion, false);

            return true;
        }

        public async Task<bool> RestablecerPass(string codigoVerificacion, string nuevaPassword)
        {
            var usuario = await _usuarioRepository.FindUserByCodeVerificacion(codigoVerificacion);
            if (usuario == null) return false;

            
            usuario.Password_Hash = PasswordHash.Hash(nuevaPassword); 
            usuario.CodigoVerificacion = null; 
            usuario.UpdateAt = DateTime.Now;

            return await _usuarioRepository.ActualizarUsuarioAsync(usuario);
        }
    }
}
