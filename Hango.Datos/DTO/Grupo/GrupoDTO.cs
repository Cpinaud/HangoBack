using Hango.Datos.DTO.Integrantes;
using Hango.Datos.DTO.Preferencia;
using Hango.Datos.EF;




namespace Hango.Datos.DTO.Grupo
{
    public class GrupoDTO
    {
        public int IdGrupo { get; set; }
        public string Nombre { get; set; }
        public string? TokenInvitacion { get; set; }
        public bool Activo { get; set; }
        public string UrlImagen { get; set; }

        public List<IntegranteDTO> Integrantes { get; set; } = new();
        public List<Zonas> Zonas { get; set; } = new();
        public List<PreferenciaDTO> Preferencias { get; set; } = new();
    }
}
