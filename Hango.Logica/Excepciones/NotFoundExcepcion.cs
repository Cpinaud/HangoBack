using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Logica.Excepciones
{
    public class NotFoundExcepcion : MasterExcepcion
    {
        public NotFoundExcepcion(string message)
        : base(message, 404) { }
    }
}
