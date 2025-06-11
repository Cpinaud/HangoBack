using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Datos.DTO.Integrantes
{
    public class IntegranteDTO
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; }
        public bool Administrador { get; set; }
    }
}
