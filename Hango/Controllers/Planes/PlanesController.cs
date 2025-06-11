using Hango.Datos.DTO.Horario;
using Hango.Datos.DTO.Planes;
using Hango.Dispatcher;
using Hango.Logica.Eventos;
using Hango.Logica.Grupos;
using Hango.Logica.Planes;
using Hango.Logica.Preferencia;
using Hango.Logica.Zona;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Hango.Controllers.Planes
{
    [ApiController]
    [Route("/Planes")]
    [ApiExplorerSettings(GroupName = "v1")]
    [Tags("4. Planes")]
    public class PlanesController : Controller
    {
        private readonly IPlanLogica _planLogica;
        private readonly IPreferenciaLogica _preferenciaLogica;
        private readonly IZonasLogica _zonasLogica;
        private readonly IGrupoLogica _grupoLogica;
        private readonly IEventoLogica _eventoLogica;
        private readonly ISignalRDispatcher _signalRDispatcher;

        public PlanesController(IPlanLogica planLogica, IPreferenciaLogica preferenciaLogica, 
                                IZonasLogica zonasLogica, IGrupoLogica grupoLogica, ISignalRDispatcher signalRDispatcher, IEventoLogica eventoLogica)
        {
            _planLogica = planLogica;
            _preferenciaLogica = preferenciaLogica;
            _zonasLogica = zonasLogica;
            _grupoLogica = grupoLogica;
            _signalRDispatcher = signalRDispatcher;
            _eventoLogica = eventoLogica;
        }


        [HttpGet("sugerencias/{idGrupo}")]
        public async Task<ActionResult<List<SugerenciaPlanDTO>>> ObtenerSugerencias(int idGrupo)
        {
            var (horarios, idsUsuarios) = await _planLogica.CalcularHorarioComunAsync(idGrupo);
            string contenidoJson;
            var idUsuario = UserLoginHelper.ObtenerIdUsuario(HttpContext.User);
            if (!horarios.Any())
            {
                var mensaje = "Los integrantes del grupo no coinciden en nigún día y horario";
                contenidoJson = JsonSerializer.Serialize(mensaje);
                await _signalRDispatcher.CrearYEmitirHub(idGrupo, "HORARIONOMATCH", contenidoJson, idUsuario);
                return NotFound("No hay horarios disponibles para ese grupo.");
            }
                

            var preferencias = await _preferenciaLogica.ObtenerPreferenciasGrupoAsync(idGrupo);
            var preferenciasTexto = preferencias.Select(p => p.Nombre).ToList();

            // Aleatorizamos las preferencias
            var random = new Random();
            preferenciasTexto = preferenciasTexto
                .OrderBy(x => random.Next())
                .Distinct()
                .Take(3)
                .ToList();

            var zonas = await _zonasLogica.ObtenerZonasPorGrupo(idGrupo);
            var nombresZonas = zonas.Select(z => z.Nombre).ToList();
            //var zona = nombresZonas.Any() ? nombresZonas[new Random().Next(nombresZonas.Count)] : "Lanus";

            var fecha = horarios.First().Fecha;

            //var clima = await _planLogica.ObtenerClimaAsync(zona, fecha);

            var climaPorZona = new Dictionary<string, string>();
            foreach (var zona in nombresZonas)
            {
                var clima = await _planLogica.ObtenerClimaAsync(zona, fecha);
                climaPorZona[zona] = clima;
            }

            var sugerencias = await _planLogica.ObtenerSugerenciasIAAsync(
                horarios,
                climaPorZona,
                preferenciasTexto,
                "$$",
                idGrupo,
                idsUsuarios,
                nombresZonas
            );

            contenidoJson = JsonSerializer.Serialize(sugerencias);
            var idEvento = await _signalRDispatcher.CrearYEmitirHub(idGrupo, "SUGERENCIA", contenidoJson,idUsuario);
            await _planLogica.AgregarIdEventoAPropuesta(sugerencias[0].PropuestaId, idEvento);
            await _eventoLogica.EliminarEventosPorTipo("HORARIONOMATCH",idGrupo);

            return Ok(); 
        }






        [HttpGet("proximos/{idUsuario}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PlanProximoDTO>>> ObtenerPlanesProximos(int idUsuario)
        {
            var planes = await _planLogica.ObtenerPlanesProximosPorUsuarioAsync(idUsuario);
            return Ok(planes);
        }

    }
}






