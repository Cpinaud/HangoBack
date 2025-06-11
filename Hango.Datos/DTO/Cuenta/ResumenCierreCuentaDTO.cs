using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Datos.DTO.Cuenta
{
    public class ResumenCierreCuentaDTO
    {
        public int IdCuenta { get; set; }
        public int? IdUsuarioCreador { get; set; }
        public string EstadoCuenta { get; set; }
        public List<ParticipanteCuentaDTO> Participaciones { get; set; }
        public DateTime? FechaCierre { get; set; }
    }
}
