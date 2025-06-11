using Hango.Datos.DTO.Cuenta;
using Hango.Datos.EF;
using Hango.Logica.Excepciones;
using Hango.Logica.Grupos;
using Hango.Logica.MercadoPago;
using Hango.Logica.Planes;
using Hango.Logica.Usuario;
using Hango.Repositorios.Cuenta;
using Hango.Repositorios.Usuario;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Logica.Cuenta
{
    public interface ICuentaLogica
    {
        
        Task<int> CrearCuentaPartesIguales(int idPlan, int idUsuarioCreador, string descripcion, decimal montoTotal, int? usuarioReceptorId, bool paguenMercadoPago);
        Task<int> CrearCuentaGastosIndividuales(int idPlan, int idUsuarioCreador, string descripcion, decimal montoTotal, bool paguenMercadoPago);
        Task<List<ParticipanteCuentaDTO>> ObtenerParticipantesDeUnaCuenta(int idCuenta);
        Task<string> RegistrarParticipacion(int idCuenta, decimal? montoGastado, int idUsuario, bool usarMercadoPago);
        Task CerrarCuentaManualmente(int idCuenta, int idUsuarioCreador);
        Task<ResumenCierreCuentaDTO> ObtenerResumenDeCuenta(int idCuenta);
        Task AgregarGastosIndividuales(int idCuenta, List<GastoIndividualRequest> gastos);
        Task<bool> TieneAutorizacion(int idUsuario);
    }

    public class CuentaLogica : ICuentaLogica
    {

        
        private readonly IGrupoLogica _grupoLogica;
        private readonly IPlanLogica _planLogica;
        private readonly IMercadoPagoLogica _mercadoPagoLogica;
        private readonly IUsuarioLogica _usuarioLogica;
        private readonly ICuentaRepositorio _cuentaRepository;

        public CuentaLogica ( IGrupoLogica grupoLogica, IPlanLogica planLogica, IMercadoPagoLogica mercadoPagoLogica, IUsuarioLogica usuarioLogica, ICuentaRepositorio cuentaRepository)
        {
          
            _grupoLogica = grupoLogica;
            _planLogica = planLogica;
            _mercadoPagoLogica = mercadoPagoLogica;
            _usuarioLogica = usuarioLogica;
            _cuentaRepository = cuentaRepository;
        }

        public async Task<int> CrearCuentaPartesIguales(int idPlan, int idUsuarioCreador, string descripcion, decimal montoTotal, int? usuarioReceptorId, bool paguenMercadoPago)
        {
            var usuarios = await _planLogica.ObtenerUsuariosIncluidosEnUnPlan(idPlan);
            if (usuarios.Count == 0)
                throw new InvalidOperationException("El grupo no tiene usuarios.");

            if (paguenMercadoPago)
            {
                var tieneAutorizacion = await TieneAutorizacion((int)usuarioReceptorId);
                if (!tieneAutorizacion)
                    throw new ErrorDeNegocioExcepcion("El receptor no tiene una autorización válida de MercadoPago.");
            }

            var cuenta = new Hango.Datos.EF.Cuenta
            {
                IdPlan = idPlan,
                UsuarioCreadorId = idUsuarioCreador,
                Descripcion = descripcion,
                MontoTotal = montoTotal,
                UsuarioReceptorId = usuarioReceptorId ?? idUsuarioCreador,
                Estado = "Abierta",
                FechaCreacion = DateTime.Now,
                ReceptorAceptaMercadoPago = paguenMercadoPago
            };

            var cuentaId = await _cuentaRepository.CrearCuentaAsync(cuenta);

            var montoPorUsuario = Math.Round(montoTotal / usuarios.Count, 2);

            await _cuentaRepository.AgregarParticipacionesAsync(cuentaId, usuarios, montoPorUsuario);
            return cuenta.IdCuenta;
        }


        public async Task<int> CrearCuentaGastosIndividuales(int idPlan, int idUsuarioCreador, string descripcion, decimal montoTotal, bool paguenMercadoPago)
        {
            var usuariosIncluidos = await _planLogica.ObtenerUsuariosIncluidosEnUnPlan(idPlan);

            if (!usuariosIncluidos.Any())
                throw new InvalidOperationException("No hay usuarios incluidos en el plan especificado.");

            if (paguenMercadoPago)
            {
                var tieneAutorizacion = await TieneAutorizacion((int)idUsuarioCreador);
                if (!tieneAutorizacion)
                    throw new ErrorDeNegocioExcepcion("El receptor no tiene una autorización válida de MercadoPago.");
            }

            var cuenta = new Hango.Datos.EF.Cuenta
            {
                IdPlan = idPlan,
                UsuarioCreadorId = idUsuarioCreador,
                Descripcion = descripcion,
                MontoTotal = montoTotal,
                Estado = "Abierta",
                FechaCreacion = DateTime.Now,
                ReceptorAceptaMercadoPago = paguenMercadoPago

            };

            var cuentaId = await _cuentaRepository.CrearCuentaAsync(cuenta);

            await _cuentaRepository.AgregarParticipacionesAsync(cuentaId, usuariosIncluidos, 0);
            return cuenta.IdCuenta;
        }

        

        public async Task<bool> TieneAutorizacion(int idUsuario)
        {
            var usuario = await _usuarioLogica.FindUserById(idUsuario); 

            if (string.IsNullOrEmpty(usuario.AccessTokenMercadoPago))
                return false;

            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", usuario.AccessTokenMercadoPago);

                var response = await httpClient.GetAsync("https://api.mercadopago.com/v1/users/me");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        public async Task CerrarCuentaManualmente(int idCuenta, int idUsuarioCreador)
        {
            var cuenta = await _cuentaRepository.FindCuentaById(idCuenta);
            var ajustes = await _cuentaRepository.ObtenerParticipacionesPorCuentaAsync(idCuenta);

            if (cuenta == null)
                throw new InvalidOperationException("La cuenta no existe.");

            if (cuenta.UsuarioCreadorId != idUsuarioCreador)
                throw new UnauthorizedAccessException("Solo el creador puede cerrar esta cuenta.");

            if (cuenta.Estado == "Cerrada")
                throw new InvalidOperationException("La cuenta ya está cerrada.");

            var participacionesActualizadas = new List<ParticipacionCuenta>();

            if (ajustes != null)
            {
                foreach (var ajuste in ajustes)
                {
                    var participacion = await _cuentaRepository.ObtenerParticipacionPorCuentaYUsuarioAsync(idCuenta, ajuste.UsuarioId);

                    if (participacion != null)
                    {
                        var montoGastado = ajuste.MontoGastado ?? 0;
                        participacion.MontoGastado = montoGastado;

                        var montoDebe = Math.Max((participacion.MontoDebe) - montoGastado, 0);
                        participacion.MontoDebe = montoDebe;

                        if (participacion.IdTipoParticipacion == 1)
                        {
                            participacion.Estado = montoDebe == 0 ? "Pagado" : "Pendiente";
                        }

                        participacionesActualizadas.Add(participacion);
                    }
                }
                await _cuentaRepository.ActualizarParticipacionesAsync(participacionesActualizadas);
            }

            var sumaGastado = ajustes.Sum(a => a.MontoGastado ?? 0);
            if (sumaGastado != cuenta.MontoTotal)
            {
                var diferencia = cuenta.MontoTotal - sumaGastado;
                cuenta.Descripcion = $"La cuenta fue cerrada con una diferencia de ${Math.Abs((decimal)diferencia):0.00}.";
            }

            cuenta.Estado = "Cerrada";
            cuenta.FechaCierre = DateTime.Now;
            await _cuentaRepository.CerrarCuentaAsync(cuenta);
        }

        public async Task<List<ParticipanteCuentaDTO>> ObtenerParticipantesDeUnaCuenta(int idCuenta)
        {
            var participantes = await _cuentaRepository.ObtenerParticipantesDeUnaCuentaAsync(idCuenta);

            return participantes;
        }


        public async Task<ResumenCierreCuentaDTO> ObtenerResumenDeCuenta(int idCuenta)
        {
            var cuenta = await _cuentaRepository.FindCuentaById(idCuenta);
            if (cuenta == null)
                throw new InvalidOperationException("La cuenta no existe.");

            var participaciones = await _cuentaRepository.ObtenerParticipantesDeUnaCuentaAsync(idCuenta);

            return new ResumenCierreCuentaDTO
            {
                IdCuenta = cuenta.IdCuenta,
                EstadoCuenta = cuenta.Estado,
                Participaciones = participaciones,
                FechaCierre = DateTime.Now
            };
        }
   
        public async Task<string> RegistrarParticipacion(int idCuenta, decimal? montoGastado, int idUsuario, bool usarMercadoPago)
        {
            var cuenta = await _cuentaRepository.FindCuentaById(idCuenta);
            if (cuenta == null)
                throw new InvalidOperationException("La cuenta no existe.");

            var participacion = await _cuentaRepository.ObtenerParticipacionPorCuentaYUsuarioAsync(idCuenta, idUsuario);
            var vendedor = await _usuarioLogica.FindUserById((int)cuenta.UsuarioReceptorId);

            if (vendedor == null || string.IsNullOrEmpty(vendedor.AccessTokenMercadoPago))
                throw new Exception("El usuario receptor no tiene vinculado un access token de MercadoPago");
            if (participacion == null)
                throw new InvalidOperationException("La participación no existe para este usuario en esta cuenta.");

            decimal montoParaPagar = montoGastado.Value;

            if (participacion.IdTipoParticipacion == 1)
            {
                participacion.MontoGastado = montoGastado ?? 0;
                
                participacion.MontoDebe = Math.Max(
                    (participacion.MontoGastado.HasValue ? participacion.MontoDebe - participacion.MontoGastado.Value : participacion.MontoDebe), 0);

                
                participacion.Estado = participacion.MontoDebe == 0 ? "Pagado" : "Pendiente";
            }
            else
            {
                participacion.MontoGastado += montoGastado ?? 0;
                montoParaPagar = participacion.MontoGastado ?? 0;
                participacion.Estado = montoParaPagar != 0 ? "Pendiente" : "Pagado";
            }

            
            string linkPago = null;
            string resultadoPago = null;
            if (montoParaPagar > 0 && usarMercadoPago)
            {
                var pagador = await _usuarioLogica.FindUserById(idUsuario);
                if (pagador == null || string.IsNullOrEmpty(pagador.AccessTokenMercadoPago))
                    throw new Exception("El usuario pagador no tiene vinculado un access token de MercadoPago");

                if (pagador.AccessTokenMercadoPago == vendedor.AccessTokenMercadoPago)
                    throw new Exception("El usuario no puede pagarse a sí mismo con MercadoPago");

          

                   linkPago = await _mercadoPagoLogica.CrearLinkDePago(vendedor.AccessTokenMercadoPago, montoParaPagar, $"Pago cuenta {idCuenta} usuario {idUsuario}", pagador.Email);

            }

            await _cuentaRepository.ActualizarParticipacionAsync(participacion);

            return linkPago;
        }



        public async Task AgregarGastosIndividuales(int idCuenta, List<GastoIndividualRequest> gastos)
        {
            var participacionesActualizadas = new List<ParticipacionCuenta>();

            foreach (var gasto in gastos)
            {
                var participacion = await _cuentaRepository.ObtenerParticipacionPorCuentaYUsuarioAsync(idCuenta, gasto.IdUsuario);

                participacion.MontoGastado = gasto.GastoRealizado;
                participacion.MontoDebe = gasto.MontoQueDebePagar;

                participacionesActualizadas.Add(participacion);
            }

            await _cuentaRepository.ActualizarParticipacionesAsync(participacionesActualizadas);
        }
    }
}