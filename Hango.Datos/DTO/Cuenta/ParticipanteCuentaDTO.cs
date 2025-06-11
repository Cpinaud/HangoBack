using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Datos.DTO.Cuenta
{
    public class ParticipanteCuentaDTO
    {

        public int IdUsuario { get; set; } 
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Estado { get; set; }
        public decimal? MontoGastado { get; set; }
        public decimal MontoDebe { get; set; }
        public string TipoParticipacion { get; set; }

    }
}
