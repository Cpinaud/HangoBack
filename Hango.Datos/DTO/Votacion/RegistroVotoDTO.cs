using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Datos.DTO.Votacion
{
    public class RegistroVotoDTO
    {
        public int IdPlan { get; set; }
        public int? Voto { get; set; }
        public int Cantidad { get; set; } = 0;
        public int Puntos { get; set; } = 0;
    }
}
