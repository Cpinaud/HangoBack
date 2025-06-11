using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Datos.DTO.Cuenta
{
    public class CrearCuentaGastosIndividualesRequest
    {
        public int IdPlan { get; set; }
        public string Descripcion { get; set; }
        public decimal MontoTotal { get; set; }
        public bool PaguenMercadoPago { get; set; }

    }
}
