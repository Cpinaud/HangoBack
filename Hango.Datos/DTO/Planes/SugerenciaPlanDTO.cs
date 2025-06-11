using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Datos.DTO.Planes
{
   public  class SugerenciaPlanDTO
    {
        public int IdPlan { get; set; }
        public int PropuestaId { get; set; }
        public int IdGrupo { get; set; }
        public int DiaSemana { get; set; }
        public DateOnly Fecha { get; set; }
        public TimeOnly Hora { get; set; }
        public string UrlImagen { get; set; }
        public string Lugar { get; set; }
        public string Direccion { get; set; }
        public string Descripcion { get; set; }
        public string? Presupuesto { get; set; }
        public List<int> UsuariosIncluidos { get; set; }
        public List<int> UsuariosDisponibles { get; set; }

        public bool UsuarioYaVotoPlan { get; set; } = false;
        public bool UsuarioParticipa { get; set; } = false;
        public bool UsuarioCompletoVotacion { get; set; } = false;
        public bool VotacionDisponible { get; set; } = false;




    }

}
