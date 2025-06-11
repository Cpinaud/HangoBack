using Hango.Datos.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Datos.DTO.Planes
{
    public class PlanDTO
    {
        public int Id { get; set; }

        public int PropuestaId { get; set; }

        public int DiaSemana { get; set; }

        public int PreferenciaId { get; set; }

        public DateOnly Fecha { get; set; }

        public TimeSpan Hora { get; set; }

        public string? Lugar { get; set; }

        public string? Descripcion { get; set; }

        public string? Direccion { get; set; }

        public string? Presupuesto { get; set; }

        public string? UrlImagen { get; set; }


    }
}
