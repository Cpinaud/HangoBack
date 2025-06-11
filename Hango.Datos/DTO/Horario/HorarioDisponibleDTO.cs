using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Datos.DTO.Horario
{
    public class HorarioDisponibleDTO
    {
        public string Dia { get; set; }           // Ej: "Miércoles"
        public DateTime Fecha { get; set; }       // Ej: 2025-06-11
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public List<int> IdUsuarios { get; set; } = new List<int>();
    }


}
