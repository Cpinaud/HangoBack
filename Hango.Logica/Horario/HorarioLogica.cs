using Hango.Datos.DTO.Horario;
using Hango.Datos.EF;
using Hango.Repositorios.Horario;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Logica.Horario
{
    public interface IHorarioLogica
    {
        Task<HorarioDisponible> GuardarHorarioDisponible(HorarioRegistroDTO horario, int i);
        Task<List<HorarioRegistroDTO>> ObtenerHorariosDisponiblePorUsuariosAsync(List<int> idsUsuarios);
        Task ActualizarHorariosAsync(Hango.Datos.EF.Usuario usuario, List<HorarioDisponibleDTO> horarioDisponibles);
        Task<List<HorarioDisponible>> ObtenerHorarioDisponibleDeUsuario(int idUsuario);

    }
    public class HorarioLogica : IHorarioLogica
    {
        private readonly IHorarioDisponibleRepositorio _horarioDisponibleRepositorio;

        public HorarioLogica(IHorarioDisponibleRepositorio horarioDisponibleRepositorio)
        {
            _horarioDisponibleRepositorio = horarioDisponibleRepositorio;
        }

        public async Task ActualizarHorariosAsync(Datos.EF.Usuario usuario, List<HorarioDisponibleDTO> horarioDisponibles)
        {
            await _horarioDisponibleRepositorio.ActualizarHorariosUsuarioAsync(usuario, horarioDisponibles);
        }

        public async Task<HorarioDisponible> GuardarHorarioDisponible(HorarioRegistroDTO horario, int idUsuario)
        {
            if (horario == null)
                throw new ArgumentNullException(nameof(horario), "El horario no puede ser nulo.");

            var entidad = new HorarioDisponible
            {
                IdUsuario = idUsuario,
                Fecha = horario.Fecha,
                DiaSemana = horario.DiaSemana,
                HorarioInicio = horario.HorarioInicio,
                HorarioFin = horario.HorarioFin
            };

            await _horarioDisponibleRepositorio.GuardarAsync(entidad);

            return entidad;
        }

        public Task<List<HorarioDisponible>> ObtenerHorarioDisponibleDeUsuario(int idUsuario)
        {
            return _horarioDisponibleRepositorio.ObtenerHorarioDisponiblePorUsuario(idUsuario);
        }

        public async Task<List<HorarioRegistroDTO>> ObtenerHorariosDisponiblePorUsuariosAsync(List<int> idsUsuarios)
        {
            if (idsUsuarios == null || !idsUsuarios.Any())
                return new List<HorarioRegistroDTO>();

            var entidades = await _horarioDisponibleRepositorio.ObtenerPorUsuariosAsync(idsUsuarios);
            return entidades.Select(h => new HorarioRegistroDTO
            {
                IdUsuario = h.IdUsuario,
                Fecha = h.Fecha,
                DiaSemana = h.DiaSemana,
                HorarioInicio = h.HorarioInicio,
                HorarioFin = h.HorarioFin
            }).ToList();
        }

    }

}
