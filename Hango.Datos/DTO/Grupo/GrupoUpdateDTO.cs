using Hango.Datos.DTO.Preferencia;
using Hango.Datos.DTO.Zona;
using Hango.Datos.EF;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;


namespace Hango.Datos.DTO.Grupo
{
    public class GrupoUpdateDTO
    {
        public int IdGrupo { get; set; }
        [StringLength(150, ErrorMessage = "El nombre no puede exceder los 150 caracteres.")] 
        public string? Nombre { get; set; }
        [MinLength(1, ErrorMessage = "Debe incluir al menos una zona.")]

        public string? UrlImagen { get; set; }
        [JsonIgnore] 
        public string? ZonasJson { get; set; }
        [JsonIgnore] 
        public string? PreferenciasJson { get; set; }

        public IFormFile? Imagen { get; set; }

        public List<ZonaDTO> Zonas =>
            string.IsNullOrEmpty(ZonasJson) ? new() : JsonConvert.DeserializeObject<List<ZonaDTO>>(ZonasJson);

        public List<PreferenciaDTO> Preferencias =>
            string.IsNullOrEmpty(PreferenciasJson) ? new() : JsonConvert.DeserializeObject<List<PreferenciaDTO>>(PreferenciasJson);
    }
}
