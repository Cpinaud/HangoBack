using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Datos.DTO.Evento
{
    public class EventoDTO
    {

        public int IdEvento { get; set; }
        public string TipoEvento { get; set; } = null!;
        public DateTime Fecha { get; set; }
        public int UsuarioId { get; set; }

        //public int IdEventoRelacionado { get; set; } FALTA DESARROLLAR
        public object? Contenido { get; set; }

    }

}
