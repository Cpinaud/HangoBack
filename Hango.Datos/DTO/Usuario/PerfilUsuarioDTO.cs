using Hango.Datos.DTO.Horario;
using Hango.Datos.DTO.Preferencia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Datos.DTO.Usuario
{
    public class PerfilUsuarioDTO
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; }
        public int IdAvatar { get; set; }
        public List<HorarioDisponibleDTO> HorariosDisponibles { get; set; }
        public byte? Presupuesto { get; set; }
        public bool HorariosPrivados { get; set; }
        public List<PreferenciaDTO> Preferencias { get; set; }
    }
}
