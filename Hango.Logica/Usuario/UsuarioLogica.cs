using Hango.Datos.DTO.Horario;
using Hango.Datos.DTO.Preferencia;
using Hango.Datos.DTO.Usuario;
using Hango.Datos.EF;
using Hango.Logica.Horario;
using Hango.Logica.Preferencia;
using Hango.Logica.Presupuesto;
using Hango.Repositorios.Presupuesto;
using Hango.Repositorios.Usuario;
using Hango.Repositorios.Utilidades;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Logica.Usuario
{
        public interface IUsuarioLogica
    {
        Task GuardarDatosUsuario(DatosUsuarioRegistroDTO user);
        Task<Hango.Datos.EF.Usuario> ObtenerUsuarioByCodigoDeVerficacion(string codigo);
        Task<PerfilUsuarioDTO> ObtenerPerfilUsuarioById(int idUsuario);
        Task<bool> EditarPerfilUsuario(PerfilUsuarioDTO perfilDto);
        Task<List<HorarioDisponible>> ObtenerHorariosDisponibleUsuarioAsync(int idUsuario);
        Task<Hango.Datos.EF.Usuario> FindUserById(int id);
        Task<Hango.Datos.EF.Usuario> FindUserByEmailAndPass(string mail, string hashPassword);
        Task<bool> ActualizarAccessTokenMercadoPago(int idUsuario, string accessToken);
        
    }

    public class UsuarioLogica : IUsuarioLogica
    {
        
        private readonly IHorarioLogica _horarioLogica;
        private readonly IPreferenciaLogica _preferenciaLogica;
        private readonly IPresupuestoLogica _presupuestoLogica;
        private readonly IUnionDeTarea _unionDeTarea;
        private readonly IUsuarioRepositorio _usuarioRepositorio;

        public UsuarioLogica(IHorarioLogica horarioLogica, IPreferenciaLogica preferenciaLogica, IPresupuestoLogica presupuestoLogica, IUnionDeTarea unionDeTarea, IUsuarioRepositorio usuarioRepositorio)
        {
            
            _horarioLogica = horarioLogica;
            _preferenciaLogica = preferenciaLogica;
            _presupuestoLogica = presupuestoLogica;
            _unionDeTarea = unionDeTarea;
            _usuarioRepositorio = usuarioRepositorio;
        }

        public async Task GuardarDatosUsuario(DatosUsuarioRegistroDTO datos)
        {
            foreach (var horario in datos.HorarioDisponible)
            {
                var nuevoHorario = new HorarioRegistroDTO
                {

                    Fecha = horario.Fecha,
                    DiaSemana = horario.DiaSemana,
                    HorarioInicio = horario.HorarioInicio,
                    HorarioFin = horario.HorarioFin
                };

                await _horarioLogica.GuardarHorarioDisponible(nuevoHorario, datos.IdUsuario);
            }

            foreach (var idPreferencia in datos.IdsPreferencias)
            {
                var nuevaPreferencia = new Usuario_Preferencia
                {
                    IdUsuario = datos.IdUsuario,
                    IdPreferencia = idPreferencia
                };

                await _preferenciaLogica.GuardarPreferenciaDelUsuario(nuevaPreferencia);
            }
            if (datos.Presupuesto != null)
            {
                int rangoNumerico = datos.Presupuesto.Rango switch
                {
                    "$" => 1,
                    "$$" => 2,
                    "$$$" => 3,
                    _ => 0 // o lanzá excepción si es un valor inválido
                };

                var presupuestoEntidad = new Hango.Datos.EF.Presupuesto
                {
                    IdUsuario = datos.IdUsuario,
                    Estimado = (decimal)datos.Presupuesto.Estimado,
                    Rango = (byte)rangoNumerico
                };

                await _presupuestoLogica.GuardarPresupuestoAsync(presupuestoEntidad);
            }



            await _unionDeTarea.GuardarCambiosAsync();
        }

        public async Task<PerfilUsuarioDTO> ObtenerPerfilUsuarioById(int idUsuario)
        {
            var usuario = await _usuarioRepositorio.ObtenerUsuarioConRelacionesAsync(idUsuario);


            if (usuario == null)
                return null;

            var perfilDto = new PerfilUsuarioDTO
            {
                IdUsuario = usuario.IdUsuario,
                Nombre = usuario.Nombre,
                IdAvatar = usuario.IdAvatar,
                HorariosPrivados = usuario.HorariosPrivados ?? false,
                Presupuesto = usuario.Presupuesto.FirstOrDefault()?.Rango,

                HorariosDisponibles = usuario.HorarioDisponible.Select(h => new HorarioDisponibleDTO
                {
                    Dia = ObtenerNombreODia(h.DiaSemana),
                    Fecha = h.Fecha,
                    HoraInicio = h.HorarioInicio,
                    HoraFin = h.HorarioFin
                }).ToList(),

                Preferencias = usuario.Usuario_Preferencia.Select(up => new PreferenciaDTO
                {
                    IdPreferencia = up.IdPreferencia,
                    Nombre = up.IdPreferenciaNavigation.Nombre
                    
                }).ToList()


            };

            return perfilDto;
          }
        public async Task<bool> EditarPerfilUsuario(PerfilUsuarioDTO dto)
        {
           
                var usuario = await _usuarioRepositorio.ObtenerUsuarioConRelacionesAsync(dto.IdUsuario);
                if (usuario == null)
                    return false;

                usuario.Nombre = dto.Nombre;
                usuario.IdAvatar = dto.IdAvatar;
                usuario.HorariosPrivados = dto.HorariosPrivados;

                await _presupuestoLogica.ActualizarPresupuestoAsync(usuario, dto.Presupuesto);
                await _horarioLogica.ActualizarHorariosAsync(usuario, dto.HorariosDisponibles);
                await _preferenciaLogica.ActualizarPreferencia(usuario, dto.Preferencias);

                await _usuarioRepositorio.GuardarCambiosAsync();
                return true;
            
        }
        public async Task<Datos.EF.Usuario> ObtenerUsuarioByCodigoDeVerficacion(string codigo)
        {
            return await _usuarioRepositorio.FindUserByCodeVerificacion(codigo);
        }


        public async Task<List<HorarioDisponible>> ObtenerHorariosDisponibleUsuarioAsync(int idUsuario)
        {
            return await _horarioLogica.ObtenerHorarioDisponibleDeUsuario(idUsuario); 
        }

      

        public async Task<Datos.EF.Usuario> FindUserById(int id)
        {
            return await _usuarioRepositorio.FindUserById(id);
        }
        public async Task<bool> ActualizarAccessTokenMercadoPago(int idUsuario, string accessToken)
        {
            var usuario = await _usuarioRepositorio.FindUserById(idUsuario);
            if (usuario == null)
                return false;

            usuario.AccessTokenMercadoPago = accessToken;

            await _usuarioRepositorio.ActualizarUsuarioAsync(usuario);
            return true;
        }

       

        public async Task<Hango.Datos.EF.Usuario> FindUserByEmailAndPass(string mail, string hashPassword)
        {
            return await _usuarioRepositorio.FindUserByPassAndEmail(mail, hashPassword);

        }
        private string ObtenerNombreODia(object dia)
        {
            return dia switch
            {
                byte b => b switch
                {
                    0 => "Domingo",
                    1 => "Lunes",
                    2 => "Martes",
                    3 => "Miércoles",
                    4 => "Jueves",
                    5 => "Viernes",
                    6 => "Sábado",
                    _ => "Día inválido"
                },
                string s => s switch
                {
                    "Domingo" => "0",
                    "Lunes" => "1",
                    "Martes" => "2",
                    "Miércoles" => "3",
                    "Jueves" => "4",
                    "Viernes" => "5",
                    "Sábado" => "6",
                    _ => throw new ArgumentException("Nombre de día inválido")
                },
                _ => throw new ArgumentException("Tipo de parámetro inválido")
            };
        }
    }
}
