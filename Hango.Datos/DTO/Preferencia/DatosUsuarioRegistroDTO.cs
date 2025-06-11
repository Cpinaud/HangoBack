using Hango.Datos.DTO.Horario;
using Hango.Datos.DTO.Presupuesto;
using Hango.Datos.EF;

namespace Hango.Datos.DTO.Preferencia
{

    
    public class DatosUsuarioRegistroDTO
    {
        public int IdUsuario { get; set; }
        public List<int> IdsPreferencias { get; set; }
        public List<HorarioRegistroDTO> HorarioDisponible { get; set; }
        public PresupuestoRegistroDTO Presupuesto { get; set; }
    }
}
