using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Datos.DTO.Cuenta
{
    public class CuentaDTO
    {
        public int GrupoId { get; set; }
        public int UsuarioCreadorId { get; set; }
        public string Descripcion { get; set; }
        public decimal MontoInicial { get; set; }

    }
}
