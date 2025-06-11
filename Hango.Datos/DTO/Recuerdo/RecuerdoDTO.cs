using NuGet.Packaging.Signing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Datos.DTO.Recuerdo
{
    public class RecuerdoDTO
    {
        public int IdPlan {  get; set; }
        public string Grupo { get; set; }
        public DateOnly Fecha { get; set; }

        public string Lugar { get; set; }
        public string Descripcion { get; set; }

    //    public string RutaImg { get; set; }
    }
}
