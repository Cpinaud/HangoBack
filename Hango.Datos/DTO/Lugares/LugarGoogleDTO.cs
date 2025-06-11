using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Datos.DTO.Lugares
{
    public class LugarGoogleDTO
    {
        public string Nombre { get; set; }
        public string Direccion { get; set; }
        public int PrecioNivel { get; set; }
        public string PlaceId { get; set; }
        public List<string> HorariosPorDia { get; set; } = new();
    }

}
