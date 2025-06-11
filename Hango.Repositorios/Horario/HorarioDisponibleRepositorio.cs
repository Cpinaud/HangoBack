using Hango.Datos.DTO.Horario;
using Hango.Datos.EF;
using Hango.Repositorios.Utilidades;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Repositorios.Horario
{
    public interface IHorarioDisponibleRepositorio
    {
        Task ActualizarHorariosUsuarioAsync(Hango.Datos.EF.Usuario usuario, List<HorarioDisponibleDTO> nuevosHorarios);
        Task<HorarioDisponible> GuardarAsync(HorarioDisponible horario);
        Task<List<HorarioDisponible>> ObtenerPorUsuariosAsync(List<int> idsUsuarios);
        Task<List<HorarioDisponible>> ObtenerHorarioDisponiblePorUsuario(int idUsuario);
    
    }
    public class HorarioDisponibleRepositorio : IHorarioDisponibleRepositorio
    {
        private readonly HangoContext _context;

        public HorarioDisponibleRepositorio(HangoContext context)
        {
            _context = context;
        }

        public async Task ActualizarHorariosUsuarioAsync(Hango.Datos.EF.Usuario usuario, List<HorarioDisponibleDTO> nuevosHorarios)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                _context.HorarioDisponible.RemoveRange(usuario.HorarioDisponible);

                usuario.HorarioDisponible = nuevosHorarios.Select(h =>
            {
                byte numeroDia = ObtenerNumeroDiaSemana(h.Dia);
                return new HorarioDisponible
                {
                    DiaSemana = numeroDia,
                    Fecha = ObtenerProximaFechaDeDia(numeroDia),
                    HorarioInicio = h.HoraInicio,
                    HorarioFin = h.HoraFin
                };
            }).ToList();
            });
            }
        public async Task<HorarioDisponible> GuardarAsync(HorarioDisponible horario)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                await _context.HorarioDisponible.AddAsync(horario);
         
                return horario;
            });
        }

        public async Task<List<HorarioDisponible>> ObtenerHorarioDisponiblePorUsuario(int idUsuario)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                return await _context.HorarioDisponible.Where(d => d.IdUsuario == idUsuario).ToListAsync();
            });
        }

        public async Task<List<HorarioDisponible>> ObtenerPorUsuariosAsync(List<int> idsUsuarios)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                return await _context.HorarioDisponible
                    .Where(h => idsUsuarios.Contains(h.IdUsuario))
                    .ToListAsync();
            });
        }
        private byte ObtenerNumeroDiaSemana(string dia)
        {
            switch (dia.Trim())
            {
                case "Domingo": return 0;
                case "Lunes": return 1;
                case "Martes": return 2;
                case "Miércoles":
                case "Miercoles": return 3;
                case "Jueves": return 4;
                case "Viernes": return 5;
                case "Sábado":
                case "Sabado": return 6;
                default: throw new ArgumentException($"Día inválido: {dia}");
            }
        }
        private DateTime ObtenerProximaFechaDeDia(byte numeroDiaSemana)
        {
            DateTime hoy = DateTime.Today;
            int diasHastaElDia = ((int)numeroDiaSemana - (int)hoy.DayOfWeek + 7) % 7;
            return hoy.AddDays(diasHastaElDia);
        }

    }
}
