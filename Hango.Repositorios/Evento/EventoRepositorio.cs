using Hango.Datos.EF;
using Hango.Repositorios.Utilidades;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Repositorios.Evento
{
    public interface IEventoRepositorio
    {
        Task GuardarEvento(Datos.EF.Evento evento);

        Task EliminarEventosPorTipo(string tipoEvento, int idGrupo);
        Task<List<Datos.EF.Evento>> ObtenerEventosPorIdGrupo(int idGrupo);
    }
        public class EventoRepositorio: IEventoRepositorio
    {

        private readonly HangoContext _context;

        public EventoRepositorio(HangoContext context)
        {
            _context = context;
        }

        public async Task EliminarEventosPorTipo(string tipoEvento, int idGrupo)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                var eventos = await _context.Evento
                    .Where(e => e.GrupoId == idGrupo && e.TipoEvento == tipoEvento)
                    .ToListAsync();
                if (eventos.Any())
                {
                    _context.Evento.RemoveRange(eventos);
                    await _context.SaveChangesAsync();
                }
            });
        }

        public async Task GuardarEvento(Datos.EF.Evento evento)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                if (evento != null)
            {
                _context.Evento.Add(evento);
                await _context.SaveChangesAsync();
            }

            });
        }

        public async Task GuardarEventoUsuario(EventoUsuario eventoU)
        {
            await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                if (eventoU != null)
                {
                    _context.EventoUsuario.Add(eventoU);
                    await _context.SaveChangesAsync();
                }

            });
        }

        public async Task<List<Datos.EF.Evento>> ObtenerEventosPorIdGrupo(int idGrupo)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                return await _context.Evento
                .Where(e => e.GrupoId == idGrupo)
                .OrderByDescending(e => e.Fecha)
                .ToListAsync();
        });
        }
}
}
