using Hango.Datos.DTO.Cuenta;
using Hango.Datos.EF;
using Hango.Repositorios.Utilidades;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Repositorios.Cuenta
{
    public interface ICuentaRepositorio
    {
        Task<int> CrearCuentaAsync(Hango.Datos.EF.Cuenta cuenta);
        Task AgregarParticipacionesAsync(int cuentaId, List<int> usuarios, decimal montoPorUsuario);
        Task<Hango.Datos.EF.Cuenta> FindCuentaById(int idCuenta);
        Task<List<ParticipacionCuenta>> ObtenerParticipacionesPorCuentaAsync(int idCuenta);
        Task ActualizarParticipacionesAsync(List<ParticipacionCuenta> participaciones);
        Task CerrarCuentaAsync(Hango.Datos.EF.Cuenta cuenta);
        Task<ParticipacionCuenta> ObtenerParticipacionPorCuentaYUsuarioAsync(int cuentaId, int usuarioId);
        Task<List<ParticipanteCuentaDTO>> ObtenerParticipantesDeUnaCuentaAsync(int idCuenta);
        Task ActualizarParticipacionAsync(ParticipacionCuenta participacion);
    }
    public class CuentaRepositorio : ICuentaRepositorio
    {
        private readonly HangoContext _context;

        public CuentaRepositorio(HangoContext context)
        {
            _context = context;
        }

        public async Task ActualizarParticipacionesAsync(List<ParticipacionCuenta> participaciones)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                _context.ParticipacionCuenta.UpdateRange(participaciones);
                await _context.SaveChangesAsync();
            });
        }
        public async Task ActualizarParticipacionAsync(ParticipacionCuenta participacion)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                _context.ParticipacionCuenta.Update(participacion);
                await _context.SaveChangesAsync();
            });
        }
        public async Task AgregarParticipacionesAsync(int cuentaId, List<int> usuarios, decimal montoPorUsuario)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                foreach (var usuarioId in usuarios)
                {
                    var participacion = new ParticipacionCuenta
                    {
                        CuentaId = cuentaId,
                        UsuarioId = usuarioId,
                        Estado = "Pendiente",
                        MontoDebe = montoPorUsuario,
                        MontoGastado = montoPorUsuario,
                        IdTipoParticipacion = 1
                    };

                    _context.ParticipacionCuenta.Add(participacion);
                }

                await _context.SaveChangesAsync();
            });
        }

        public async Task CerrarCuentaAsync(Datos.EF.Cuenta cuenta)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                _context.Cuenta.Update(cuenta);
                await _context.SaveChangesAsync();
            });
        }

        public async Task<int> CrearCuentaAsync(Datos.EF.Cuenta cuenta)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                _context.Cuenta.Add(cuenta);
                await _context.SaveChangesAsync();
                return cuenta.IdCuenta;
            });
        }

        public async Task<Datos.EF.Cuenta> FindCuentaById(int idCuenta)
        {
            var cuenta = await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                return await _context.Cuenta.FindAsync(idCuenta);
            });
            if (cuenta == null)
                throw new Exception("Cuenta no encontrada");
            return cuenta;
        }

        public async Task<List<ParticipacionCuenta>> ObtenerParticipacionesPorCuentaAsync(int idCuenta)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                return await _context.ParticipacionCuenta
                                     .Where(pc => pc.CuentaId == idCuenta)
                                     .ToListAsync();
            });
        }
        public async Task<ParticipacionCuenta> ObtenerParticipacionPorCuentaYUsuarioAsync(int cuentaId, int usuarioId)
        {
            var cuenta = await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                return await _context.ParticipacionCuenta
                    .FirstOrDefaultAsync(p => p.CuentaId == cuentaId && p.UsuarioId == usuarioId);
            });
            if (cuenta == null)
                throw new Exception("Cuenta no encontrada");
            return cuenta;
        }
        public async Task<List<ParticipanteCuentaDTO>> ObtenerParticipantesDeUnaCuentaAsync(int idCuenta)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                return await _context.ParticipacionCuenta
                    .Where(p => p.CuentaId == idCuenta)
                    .Include(p => p.Usuario)
                    .Include(p => p.IdTipoParticipacionNavigation) 
                    .Select(p => new ParticipanteCuentaDTO
                    {
                        IdUsuario = p.UsuarioId,
                        Nombre = p.Usuario.Nombre,
                        Email = p.Usuario.Email,
                        Estado = p.Estado,
                        MontoGastado = p.MontoGastado,
                        MontoDebe = p.MontoDebe,
                        TipoParticipacion = p.IdTipoParticipacionNavigation.Nombre
                    })
                    .ToListAsync();
            });
        }
    }
}
