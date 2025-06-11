using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Datos.DTO.Usuario
{
    public class MatchesDTO
    {
        public int IdGrupo { get; set; }
        public string NombreGrupo { get; set; }
        public string DiaSemana { get; set; }
        public TimeSpan HoraDesde { get; set; }
        public TimeSpan HoraHasta { get; set; }
        public List<UsuarioDTO> UsuarioConMatch { get; set; }
    }
}
