using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Datos.DTO.Votacion
{
    public class VotacionPlanDTO
    {
        public int IdPlan { get; set; }

        public int IdPropuesta { get; set; }
        public int? Voto { get; set; }  // 0=Me da igual, 1=No me gusta, 2=Me gusta, 3= no participo

        public int IdGrupo { get; set; }

        public bool? VotacionCompleta { get; set; } = false;
        public bool? VotacionAnulada { get; set; } = false;
        public bool? UsuarioNoParticipa { get; set; } = false;

        public int IdEventoSugerenciaPlan { get; set; }

    }
}
