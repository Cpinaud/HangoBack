using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Datos.DTO.Cuenta
{
    public class GastoIndividualRequest
    {
        public int IdUsuario { get; set; }
        public decimal GastoRealizado { get; set; }
        public decimal MontoQueDebePagar { get; set; }
    }
}
