using Hango.Datos.DTO.Grupo;
using Hango.Datos.EF;
using Hango.Hubs;
using Hango.Logica.Eventos;
using Microsoft.AspNetCore.SignalR;

namespace Hango.Dispatcher
{
    public interface ISignalRDispatcher
    {
        Task<int> CrearYEmitirHub(int idGrupo, string tipo, string contenidoJson, int idUsuario);
        Task NotificarActualizacionIntegrantes(int idGrupo);

        Task NotificarActualizacionesVotacion(int idGrupo);
    }
    public class SignalRDispatcher : ISignalRDispatcher
    {
        private readonly IEventoLogica _eventoLogica;
        private readonly IHubContext<GrupoHub> _hubContext;

        public SignalRDispatcher(IEventoLogica eventoLogica, IHubContext<GrupoHub> hubContext)
        {
            _eventoLogica = eventoLogica;
            _hubContext = hubContext;
        }

        public async Task<int> CrearYEmitirHub(int idGrupo, string tipo, string contenidoJson, int idUsuario)
        {
            var evento= await _eventoLogica.CrearEvento(idGrupo, tipo, contenidoJson, idUsuario);
            await _hubContext
                .Clients
                .Group(idGrupo.ToString())
                .SendAsync("RecibirEvento", contenidoJson);

            return evento.IdEvento;
        }

        public async Task NotificarActualizacionIntegrantes(int idGrupo)
        {
            await _hubContext
                .Clients
                .Group(idGrupo.ToString())
                .SendAsync("ActualizarIntegrantes", idGrupo.ToString());

        }

        public async Task NotificarActualizacionesVotacion(int idGrupo)
        {
            await _hubContext
                .Clients
                .Group(idGrupo.ToString())
                .SendAsync("ActualizarVotacion", idGrupo.ToString());

        }


    }
}
