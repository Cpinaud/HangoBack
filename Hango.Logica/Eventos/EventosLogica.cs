using Hango.Datos.DTO.Cuenta;
using Hango.Datos.DTO.Evento;
using Hango.Datos.DTO.Planes;
using Hango.Datos.EF;
using Hango.Repositorios.Evento;
using Hango.Repositorios.Grupo;
using Hango.Repositorios.Votacion;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hango.Logica.Eventos
{
    public interface IEventoLogica
    {
        Task<List<EventoDTO>> ObtenerEventosPorGrupo(int idGrupo, int idUsuario);

        Task<Evento> CrearEvento(int idGrupo,string tipoEvento, string contenido, int idUsuario);

        Task EliminarEventosPorTipo(string tipoEvento,int idGrupo);
    }
    public class EventoLogica : IEventoLogica
    {
      
        private readonly IGrupoRepositorio _grupoRepositorio;
        private readonly IEventoRepositorio _eventoRepositorio;
        private readonly IVotacionRepositorio _votacionRepositorio;

        public EventoLogica(IGrupoRepositorio grupoRepositorio, IEventoRepositorio eventoRepositorio,IVotacionRepositorio votacionRepositorio )
        {
            _grupoRepositorio = grupoRepositorio;
            _eventoRepositorio = eventoRepositorio;
            _votacionRepositorio = votacionRepositorio;
        }

        public async Task<Evento> CrearEvento(int idGrupo, string tipoEvento, string contenido, int idUsuario)
        {
            try
            {
                var evento = new Evento
                {
                    GrupoId = idGrupo,
                    UsuarioId = idUsuario,
                    Fecha = DateTime.Now,
                    TipoEvento = tipoEvento,
                    Contenido = contenido
                    //IdEventoRelacionado = idEventoRelacionado //FALTA DESARROLLAR
                };
                await _eventoRepositorio.GuardarEvento(evento);
                var eventoUsuario = new EventoUsuario
                {
                    EventoId = evento.IdEvento,
                    UsuarioId = idUsuario,
                    Leido = false
                };
                
                return await Task.FromResult(evento);
            }
            catch (Exception)
            {
                throw new Exception("Error al crear el evento.");

            }
        }

        public async Task EliminarEventosPorTipo(string tipoEvento,int idGrupo)
        {
            await _eventoRepositorio.EliminarEventosPorTipo(tipoEvento, idGrupo);
        }

        public async Task<List<EventoDTO>> ObtenerEventosPorGrupo(int idGrupo,int idUsuario)
        {
            try
            {
                var usuarioPerteneceAlGrupo = await _grupoRepositorio.UsuarioPerteneceAlGrupo(idUsuario, idGrupo);
                if (!usuarioPerteneceAlGrupo)
                    throw new Exception("El usuario no pertenece al grupo especificado.");

                var eventosList = await _eventoRepositorio.ObtenerEventosPorIdGrupo(idGrupo);

                var eventos = new List<EventoDTO>();

                foreach (var evento in eventosList)
                {
                    object? contenidoParseado = null;

                    switch (evento.TipoEvento)
                    {
                        case "MENSAJE":
                            contenidoParseado = JsonSerializer.Deserialize<string>(evento.Contenido);
                            break;
                        case "HORARIONOMATCH":
                            contenidoParseado = JsonSerializer.Deserialize<string>(evento.Contenido);
                            break;
                        case "SUGERENCIA":
                            var sugerencias = JsonSerializer.Deserialize<List<SugerenciaPlanDTO>>(evento.Contenido);

                            foreach (var s in sugerencias)
                            {
                         
                                s.UsuarioYaVotoPlan = await _votacionRepositorio.UsuarioVotoPlan(idUsuario,s.IdPlan);
                                s.UsuarioParticipa = s.UsuariosIncluidos.Contains(idUsuario);
                                s.UsuarioCompletoVotacion = await _votacionRepositorio.ValidarFinVotacionUsuario(s.PropuestaId,idUsuario);
                                s.VotacionDisponible = (await _votacionRepositorio.ObtenerPlanElegidoPorIdPropuesta(s.PropuestaId)==null &&  _votacionRepositorio.ObtenerFechaVtoDePropuesta(s.PropuestaId).Result>DateTime.Now);
                            }

                            contenidoParseado = sugerencias;
                            break;

                            break;
                        case "CUENTA":
                            contenidoParseado = JsonSerializer.Deserialize<CuentaDTO>(evento.Contenido);
                            break;
                        case "PLANCONCRETADO":
                            contenidoParseado = JsonSerializer.Deserialize<PlanDTO>(evento.Contenido);
                            break;
                    }

                    eventos.Add(new EventoDTO
                    {
                        IdEvento = evento.IdEvento,
                        TipoEvento = evento.TipoEvento,
                        Fecha = evento.Fecha,
                        UsuarioId = evento.UsuarioId,
                        //IdEventoRelacionado = evento.IdEventoRelacionado, //FALTA DESARROLLAR
                        Contenido = contenidoParseado
                    });
                    
                }
                return eventos;
            }
            catch (Exception)
            {
                throw new Exception("Error al obtener los eventos del grupo.");
            }

        }


    }
}
