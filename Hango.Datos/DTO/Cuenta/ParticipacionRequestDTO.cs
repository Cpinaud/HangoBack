using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Datos.DTO.Cuenta
{
    public class ParticipacionRequestDTO
    {

        public int IdUsuario { get; set; }
  //      public string EstadoParticipacion { get; set; }
        public decimal? MontoGastado { get; set; }
    }
}
