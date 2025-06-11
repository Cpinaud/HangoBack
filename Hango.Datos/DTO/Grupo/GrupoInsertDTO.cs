using Hango.Datos.EF;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;


namespace Hango.Datos.DTO.Grupo
{
    public class GrupoInsertDTO
    {
        [StringLength(150, ErrorMessage = "El nombre no puede exceder los 150 caracteres.")]
        public string Nombre { get; set; }

        [MinLength(1, ErrorMessage = "Debe incluir al menos una zona.")]
        public List<string> Zonas { get; set; } = new();
        public IFormFile? Imagen { get; set; }
        
    }
}
