using Hango.Datos.EF;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Hango.Datos.DTO.Horario;
using Hango.Datos.DTO;
using Hango.Logica.Horario;
using Hango.Logica.Preferencia;
using Hango.Datos.DTO.Planes;
using System.Numerics;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json;
using Hango.Datos.DTO.Lugares;


namespace Hango.Logica.Planes
{

    public interface IPlanLogica
    {
        Task<(List<HorarioDisponibleDTO> Horarios, List<int> IdUsuarios)> CalcularHorarioComunAsync(int idGrupo);
        Task<string> ObtenerClimaAsync(string ciudad, DateTime fechaHora);
        Task<List<SugerenciaPlanDTO>> ObtenerSugerenciasIAAsync(
            List<HorarioDisponibleDTO> horariosDisponibles,
          //string clima,
            Dictionary<string, string> climaPorZona,
            List<string> preferencias,
            string presupuesto,
            int idGrupo,
            List<int> idsUsuarios,
            List<string> zonas
        );
        Task<List<PlanProximoDTO>> ObtenerPlanesProximosPorUsuarioAsync(int idUsuario);
        Task<List<int>> ObtenerUsuariosIncluidosEnUnPlan(int idPlan);

        Task AgregarIdEventoAPropuesta(int idPropuesta,int idEvento);

    }

    public class PlanLogica : IPlanLogica
    {
        private readonly HangoContext _context;
        private readonly IHorarioLogica _horarioLogica;
        private readonly IPreferenciaLogica _preferenciaLogica;
        private readonly string _apiKey;
        private readonly string _openWeatherApiKey;
        private readonly string _googlePlacesApiKey;


        public PlanLogica(HangoContext context, IHorarioLogica horarioLogica, IPreferenciaLogica preferenciaLogica, IConfiguration configuration)
        {
            _context = context;
            _horarioLogica = horarioLogica;
            _preferenciaLogica = preferenciaLogica;
            _apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("La clave de la API de OpenAI no está configurada.");
            _openWeatherApiKey = configuration["OpenWeather:ApiKey"] ?? throw new InvalidOperationException("La clave de la API de OpenWeather no está configurada.");
            _googlePlacesApiKey = configuration["GoogleApi:PlacesApiKey"] ?? throw new InvalidOperationException("La clave de la API de Google Places no está configurada.");
        }


        public async Task<(List<HorarioDisponibleDTO> Horarios, List<int> IdUsuarios)> CalcularHorarioComunAsync(int idGrupo)
        {
            var idsUsuarios = await _context.Usuario_Grupo
                .Where(ug => ug.IdGrupo == idGrupo)
                .Select(ug => ug.IdUsuario)
                .ToListAsync();

            var horariosDisponibles = new List<HorarioDisponibleDTO>();

            if (!idsUsuarios.Any())
                return (horariosDisponibles, idsUsuarios);

            var horariosPorUsuario = await _horarioLogica.ObtenerHorariosDisponiblePorUsuariosAsync(idsUsuarios);

            var fechasUnicas = horariosPorUsuario.Select(h => h.Fecha.Date).Distinct();

            foreach (var fecha in fechasUnicas)
            {
                var horariosDelDia = horariosPorUsuario.Where(h => h.Fecha.Date == fecha).ToList();
                var usuarios = horariosDelDia.Select(h => h.IdUsuario).Distinct();

                var tramosPorUsuario = usuarios.ToDictionary(
                    id => id,
                    id => horariosDelDia
                            .Where(h => h.IdUsuario == id)
                            .Select(h => (h.HorarioInicio, h.HorarioFin))
                            .ToList()
                );

                var bloques = new List<(TimeSpan inicio, TimeSpan fin)>();
                var hora = TimeSpan.FromHours(0);
                while (hora < TimeSpan.FromHours(24))
                {
                    var bloqueInicio = hora;
                    var bloqueFin = hora + TimeSpan.FromMinutes(30);
                    if (bloqueFin > TimeSpan.FromHours(24)) break;

                    int disponibles = usuarios.Count(uid =>
                        tramosPorUsuario[uid].Any(t =>
                            t.HorarioInicio <= bloqueInicio && t.HorarioFin >= bloqueFin
                        )
                    );

                    if (disponibles >= 2)
                        bloques.Add((bloqueInicio, bloqueFin));

                    hora += TimeSpan.FromMinutes(30);
                }

                var bloquesUnidos = UnirBloquesContiguosConUsuarios(bloques, tramosPorUsuario);

                var ahora = DateTime.Now;
                var minimoPermitido = ahora.AddHours(4);


                foreach (var b in bloquesUnidos)
                {
                    if (b.usuarios.Count >= 2)
                    {
                        var fechaHoraInicio = fecha.Date + b.inicio;
                        if (fechaHoraInicio >= minimoPermitido) // este es el filtro clave
                        {
                            horariosDisponibles.Add(new HorarioDisponibleDTO
                            {
                                Dia = fecha.ToString("dddd", new CultureInfo("es-ES")),
                                Fecha = fecha,
                                HoraInicio = b.inicio,
                                HoraFin = b.fin,
                                IdUsuarios = b.usuarios
                            });
                        }
                    }
                }
            }
            return (horariosDisponibles, idsUsuarios);
        }



        public async Task<string> ObtenerClimaAsync(string ciudad, DateTime fechaHora)
        {
            string apiKey = _openWeatherApiKey;
            string ciudadFormateada = $"{ciudad},AR";
            string url = $"https://api.openweathermap.org/data/2.5/forecast?q={ciudadFormateada}&appid={apiKey}&units=metric&lang=es";

            using HttpClient httpClient = new();

            try
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement lista = doc.RootElement.GetProperty("list");

                JsonElement? entradaCercana = null;
                TimeSpan diferenciaMinima = TimeSpan.MaxValue;

                foreach (var item in lista.EnumerateArray())
                {
                    string? dt_txt = item.GetProperty("dt_txt").GetString();
                    DateTime fechaItem = DateTime.Parse(dt_txt!);

                    if (fechaItem.Date == fechaHora.Date)
                    {
                        TimeSpan diferencia = (fechaItem - fechaHora).Duration();

                        if (diferencia < diferenciaMinima)
                        {
                            diferenciaMinima = diferencia;
                            entradaCercana = item;
                        }
                    }
                }

                if (entradaCercana.HasValue)
                {
                    var elegido = entradaCercana.Value;
                    string descripcion = elegido.GetProperty("weather")[0].GetProperty("description").GetString()!;
                    double temperatura = elegido.GetProperty("main").GetProperty("temp").GetDouble();

                    return $"{descripcion}, {temperatura}°C";
                }

                return "Clima no disponible para esa fecha.";
            }
            catch (Exception ex)
            {
                return $"Error al obtener el clima: {ex.Message}";
            }
        }





        public async Task<List<SugerenciaPlanDTO>> ObtenerSugerenciasIAAsync(
            List<HorarioDisponibleDTO> horariosDisponibles,
          //string clima,
            Dictionary<string, string> climaPorZona,
            List<string> preferencias,
            string presupuesto,
            int idGrupo,
            List<int> idsUsuarios,
            List<string> zonas)
        {

                string apiKeyGooglePlaces = _googlePlacesApiKey;
            var random = new Random();


            if (idsUsuarios == null || idsUsuarios.Count <= 1)
            {
                throw new InvalidOperationException("El grupo debe tener al menos 2 integrantes para generar sugerencias de planes.");
            }

            var url = "https://api.openai.com/v1/chat/completions";

            var horariosTexto = string.Join("\n", horariosDisponibles.Select(h =>
            {
                var duracion = h.HoraFin - h.HoraInicio;
                var duracionStr = $"{(int)duracion.TotalHours} horas y {duracion.Minutes} minutos";
                return $"- Día: {h.Dia} {h.Fecha:dd} → {h.HoraInicio:hh\\:mm} a {h.HoraFin:hh\\:mm} ({duracionStr})";
            }));

            var climaTexto = string.Join("\n", climaPorZona.Select(kvp => $"- {kvp.Key}: {kvp.Value}"));


            var promptUsuario = $"""
Dado el siguiente contexto:
Horarios posibles para salir:
{horariosTexto}
Clima por zona:
{climaTexto}
Preferencias del grupo: {string.Join(", ", preferencias)}
Zonas disponibles: {string.Join(", ", zonas)}
Presupuesto: {presupuesto}

GENERÁ EXACTAMENTE 3 PLANES GRUPALES POSIBLES (ni más, ni menos) , eligiendo entre los horarios disponibles.
Cada plan debe indicar cuál de los horarios usa, a qué preferencia pertenece y una de las zonas disponibles.
No es necesario usar una zona distinta en cada plan.
Cada plan debe usar una preferencia diferente. No repitas la misma preferencia en distintos planes.

Tené en cuenta el clima de la zona seleccionada para el plan. Si está lluvioso, no propongas actividades al aire libre.

**El campo "Plan" debe ser un título atractivo y breve de la actividad, sin incluir lugar, zona, fecha ni hora.**

Si la preferencia se puede asociar a un tipo de lugar que pueda buscarse con la API de Google Places, agregá un campo adicional llamado `TipoLugarGoogle` con el valor correspondiente (por ejemplo: "movie_theater", "cafe", "park", etc). Si no aplica, omitilo.

Formato ESTRICTO (sin explicaciones), uno por línea, este sería un ejemplo:
Plan: Merienda divertida  
Preferencia: Merienda  
Zona: Ramos Mejía  
Presupuesto: $$  
Horario y día: Miércoles 21 10:00hs  
TipoLugarGoogle: cafe
""";

            var requestBody = new
            {
                model = "gpt-4.1-mini",
                messages = new[]
                {
            new { role = "system", content = "Sos un asistente que propone salidas grupales en base a datos." },
            new { role = "user", content = promptUsuario }
        },
                max_tokens = 300,
                temperature = 0.7
            };

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            dynamic result = JsonConvert.DeserializeObject(responseString);
            var respuestaIA = result?.choices?[0]?.message?.content?.ToString();

            if (string.IsNullOrEmpty(respuestaIA))
                return new List<SugerenciaPlanDTO>();

            var lineas = respuestaIA.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var sugerencias = new List<SugerenciaPlanDTO>();

            var propuesta = new Propuesta
            {
                GrupoId = idGrupo,
                FechaCreacion = DateTime.Now,
                FechaVencimiento = DateTime.Now.AddDays(2),
                Origen = "IA"
            };

            _context.Propuesta.Add(propuesta);
            await _context.SaveChangesAsync();

            foreach (var idUsuario in idsUsuarios)
            {
                var usuarioPropuesta = new Usuario_Propuesta
                {
                    IdUsuario = idUsuario,
                    IdPropuesta = propuesta.IdPropuesta
                };
                _context.Usuario_Propuesta.Add(usuarioPropuesta);
            }
            await _context.SaveChangesAsync();


            for (int i = 0; i < lineas.Length; i += 6)
            {
                if (i + 5 >= lineas.Length) break;

                var planDesc = lineas[i].Replace("Plan:", "").Trim();
                var preferencia = lineas[i + 1].Replace("Preferencia:", "").Trim();
                var zonaResp = lineas[i + 2].Replace("Zona:", "").Trim();
                var presupuestoResp = lineas[i + 3].Replace("Presupuesto:", "").Trim();
                var horario = lineas[i + 4].Replace("Horario y día:", "").Trim();
                var tipoLugarGoogle = lineas[i + 5].Replace("TipoLugarGoogle:", "").Trim();

                var partes = horario.Split(" ");
                if (partes.Length < 3) continue;

                string dia = partes[0];
                string fechaTexto = partes[1];
                var horaTexto = partes[2].Replace("hs", "").Trim();

                TimeSpan horaParseada;
                TimeSpan.TryParse(horaTexto, out horaParseada);


                int diaMes = int.TryParse(fechaTexto, out int d) ? d : DateTime.Now.Day;
                DateOnly fecha = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, diaMes);
                int preferenciaId = await _preferenciaLogica.ObtenerIdPreferenciaPorNombre(preferencia);


                int? presupuestoMax = presupuestoResp switch
                {
                    "$" => 1,
                    "$$" => 2,
                    "$$$" => 3,
                    "$$$$" => 4,
                    _ => null
                };

                var lugares = await BuscarLugaresConDetallesAsync(tipoLugarGoogle, zonaResp, apiKeyGooglePlaces, presupuestoMax ?? 4);
                var lugarAleatorio = lugares.Count > 0 ? lugares[random.Next(lugares.Count)] : null;


                var plan = new Datos.EF.Planes
                {
                    PropuestaId = propuesta.IdPropuesta,
                    //DiaSemana = (int)DateTime.Now.DayOfWeek,  Descomentar esta linea si la linea de abajo trae problemas
                    DiaSemana = (int)new DateTime(fecha.Year, fecha.Month, fecha.Day).DayOfWeek,
                    Fecha = fecha,
                    //En las clases los campos TimeOnly hay que cambiarlos a mano a TimeSpan
                    //Hora = TimeOnly.FromTimeSpan(horaParseada),
                    Hora = horaParseada,
                    Lugar = lugarAleatorio?.Nombre ?? zonaResp,
                    Direccion = lugarAleatorio?.Direccion,
                    PreferenciaId = preferenciaId,
                    Descripcion = planDesc,
                    Presupuesto = presupuestoResp,
                    Estado = "0" // Estado inicial, "Pendiente" (confirmado - Cancelado)
                };

                _context.Planes.Add(plan);
                await _context.SaveChangesAsync();


                var usuariosCoincidenConPlan = await _horarioLogica.ObtenerHorariosDisponiblePorUsuariosAsync(idsUsuarios);

                var usuariosQueCoinciden = usuariosCoincidenConPlan
                    .Where(h =>
                        h.Fecha.Day == fecha.Day &&
                        h.Fecha.Month == fecha.Month &&
                        h.Fecha.Year == fecha.Year &&
                        //los campos en las clases hay que cambiarlos a TimeSpan cuando hacemos el scope
                        //plan.Hora.ToTimeSpan() >= h.HorarioInicio &&
                        //plan.Hora.ToTimeSpan() < h.HorarioFin
                        plan.Hora >= h.HorarioInicio &&
                        plan.Hora < h.HorarioFin
                    )
                    .Select(h => h.IdUsuario)
                    .Distinct()
                    .ToList();

                foreach (var idUsuario in usuariosQueCoinciden)
                {
                    var usuarioPlan = new Usuario_Plan
                    {
                        UsuarioId = idUsuario,
                        PlanId = plan.Id,
                        Estado = "DISPONIBLE"
                    };

                    _context.Usuario_Plan.Add(usuarioPlan);
                }

                var usuariosNoDisponibles = idsUsuarios.Except(usuariosQueCoinciden);
                foreach (var idUsuario in usuariosNoDisponibles)
                {
                    var usuarioPlan = new Usuario_Plan
                    {
                        UsuarioId = idUsuario,
                        PlanId = plan.Id,
                        Estado = "NO DISPONIBLE"
                    };

                    _context.Usuario_Plan.Add(usuarioPlan);
                }

                await _context.SaveChangesAsync();


                // actualizar vencimiento de la propuesta
                var fechaHoraVtoPlan = plan.Fecha.ToDateTime(TimeOnly.FromTimeSpan(plan.Hora)).AddHours(-3);
                if (i == 0)
                    propuesta.FechaVencimiento = fechaHoraVtoPlan; 
                else
                {
                    if (fechaHoraVtoPlan< propuesta.FechaVencimiento)
                    {
                        propuesta.FechaVencimiento = fechaHoraVtoPlan;
                    }
                }
                _context.Propuesta.Update(propuesta);
                await _context.SaveChangesAsync();


                sugerencias.Add(new SugerenciaPlanDTO
                {
                    PropuestaId = propuesta.IdPropuesta,
                    IdGrupo = idGrupo,
                    UrlImagen = $"/preferencias/{preferenciaId}.png",
                    IdPlan = plan.Id,
                    Descripcion = planDesc,
                    Lugar = lugarAleatorio?.Nombre ?? zonaResp,
                    Direccion = lugarAleatorio?.Direccion,
                    Presupuesto = presupuestoResp,
                    DiaSemana = dia.Length > 0 ? Array.IndexOf(new[] { "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" }, dia) : (int)DateTime.Now.DayOfWeek,
                    Fecha = fecha,
                    Hora = TimeOnly.FromTimeSpan(horaParseada),
                    UsuariosIncluidos = idsUsuarios,
                    UsuariosDisponibles = usuariosQueCoinciden
                });

            }

            return sugerencias;
        }



        public static async Task<List<LugarGoogleDTO>> BuscarLugaresConDetallesAsync(string query, string zona, string apiKey, int presupuestoMaximo)
        {
            var resultados = new List<LugarGoogleDTO>();

            using var httpClient = new HttpClient();

            var urlBusqueda = $"https://maps.googleapis.com/maps/api/place/textsearch/json?" +
                              $"query={Uri.EscapeDataString(query + " en " + zona)}&key={apiKey}";

            var responseBusqueda = await httpClient.GetAsync(urlBusqueda);
            var jsonBusqueda = await responseBusqueda.Content.ReadAsStringAsync();
            dynamic resultadoBusqueda = JsonConvert.DeserializeObject(jsonBusqueda);

            foreach (var lugar in resultadoBusqueda.results)
            {
                int precioNivel = lugar?.price_level != null ? (int)lugar.price_level : -1;
                if (precioNivel > presupuestoMaximo) continue;

                string placeId = lugar.place_id;

                var urlDetalles = $"https://maps.googleapis.com/maps/api/place/details/json?place_id={placeId}&fields=opening_hours&key={apiKey}";
                var responseDetalles = await httpClient.GetAsync(urlDetalles);
                var jsonDetalles = await responseDetalles.Content.ReadAsStringAsync();
                dynamic detalles = JsonConvert.DeserializeObject(jsonDetalles);

                List<string> horarios = new();
                if (detalles?.result?.opening_hours?.weekday_text != null)
                {
                    foreach (var dia in detalles.result.opening_hours.weekday_text)
                    {
                        horarios.Add((string)dia);
                    }
                }

                resultados.Add(new LugarGoogleDTO
                {
                    Nombre = lugar.name,
                    Direccion = lugar.formatted_address,
                    PrecioNivel = precioNivel,
                    PlaceId = placeId,
                    HorariosPorDia = horarios
                });
            }

            return resultados;
        }



        public async Task<List<PlanProximoDTO>> ObtenerPlanesProximosPorUsuarioAsync(int idUsuario)
        {
            var ahora = DateTime.Now;

            var planesUsuario = await _context.Planes
                .Include(p => p.Propuesta)
                    .ThenInclude(propuesta => propuesta.Grupo)
                .Where(p => p.Usuario_Plan.Any(up => up.UsuarioId == idUsuario))
                .ToListAsync();


            var planes = planesUsuario
                .Where(p => p.Fecha.ToDateTime(TimeOnly.FromTimeSpan(p.Hora)) > ahora)
                .OrderBy(p => p.Fecha)
                .ThenBy(p => p.Hora)
                .Select(p => new PlanProximoDTO
                {
                    Plan = p.Descripcion ?? "Plan sin descripción",
                    Zona = p.Lugar ?? "Zona no especificada",
                    Presupuesto = p.Presupuesto ?? "Sin presupuesto",
                    Dia = p.Fecha.ToDateTime(TimeOnly.FromTimeSpan(p.Hora)).ToString("dddd", new CultureInfo("es-ES")),
                    FechaTexto = p.Fecha.ToString("dd/MM/yyyy"),
                    Hora = p.Hora.ToString("HH:mm"),
                    NombreGrupo = p.Propuesta.Grupo.Nombre
                })
                .ToList();

            return planes;

        }


        //FUNCIONES AUXILIARES
        private DateTime ObtenerProximaFecha(string nombreDia)
        {
            var dias = new[] { "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };
            var hoy = DateTime.Today;
            int diaObjetivo = Array.IndexOf(dias, nombreDia);

            for (int i = 0; i < 7; i++)
            {
                var fecha = hoy.AddDays(i);
                if ((int)fecha.DayOfWeek == diaObjetivo)
                    return fecha;
            }

            return hoy;
        }

        public async Task<List<int>> ObtenerUsuariosIncluidosEnUnPlan(int idPlan)
        {
            var usuariosIncluidos = await _context.Usuario_Plan
                                .Where(up => up.PlanId == idPlan && up.Estado == "INCLUIDO")
                                .Select(up => up.UsuarioId)
                                .Distinct()
                                .ToListAsync();

            return usuariosIncluidos;

        }

        private List<(TimeSpan inicio, TimeSpan fin, List<int> usuarios)> UnirBloquesContiguosConUsuarios(
    List<(TimeSpan inicio, TimeSpan fin)> bloques,
    Dictionary<int, List<(TimeSpan inicio, TimeSpan fin)>> tramosPorUsuario)
        {
            var resultado = new List<(TimeSpan inicio, TimeSpan fin, List<int> usuarios)>();

            if (!bloques.Any()) return resultado;

            var actualInicio = bloques[0].inicio;
            var actualFin = bloques[0].fin;
            var usuariosActuales = ObtenerUsuariosDisponibles(tramosPorUsuario, actualInicio, actualFin);

            for (int i = 1; i < bloques.Count; i++)
            {
                var siguienteInicio = bloques[i].inicio;
                var siguienteFin = bloques[i].fin;
                var usuariosSiguientes = ObtenerUsuariosDisponibles(tramosPorUsuario, siguienteInicio, siguienteFin);

                if (siguienteInicio == actualFin && usuariosSiguientes.OrderBy(x => x).SequenceEqual(usuariosActuales.OrderBy(x => x)))
                {
                    actualFin = siguienteFin;
                }
                else
                {
                    resultado.Add((actualInicio, actualFin, usuariosActuales));
                    actualInicio = siguienteInicio;
                    actualFin = siguienteFin;
                    usuariosActuales = usuariosSiguientes;
                }
            }

            resultado.Add((actualInicio, actualFin, usuariosActuales));
            return resultado;
        }

        private List<int> ObtenerUsuariosDisponibles(Dictionary<int, List<(TimeSpan inicio, TimeSpan fin)>> tramosPorUsuario, TimeSpan inicio, TimeSpan fin)
        {
            return tramosPorUsuario
                .Where(kvp => kvp.Value.Any(t => t.inicio <= inicio && t.fin >= fin))
                .Select(kvp => kvp.Key)
                .ToList();
        }


        public async Task AgregarIdEventoAPropuesta(int idPropuesta, int idEvento)
        {
            var propuesta = await _context.Propuesta
                .Where(p => p.IdPropuesta == idPropuesta)
                .FirstOrDefaultAsync();
            if (propuesta != null)
            {
                propuesta.IdEvento = idEvento;
                _context.Propuesta.Update(propuesta);
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new InvalidOperationException("La propuesta no existe o no se encontró.");
            }
        }

    }

}
