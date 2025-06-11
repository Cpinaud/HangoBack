using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Datos.DTO.Horario
{
    public class HorarioRegistroDTO
    {
        public int IdUsuario { get; set; }
        public DateTime Fecha { get; set; }
        public byte DiaSemana { get; set; }
        public TimeSpan HorarioInicio { get; set; }
        public TimeSpan HorarioFin { get; set; }
    }
}