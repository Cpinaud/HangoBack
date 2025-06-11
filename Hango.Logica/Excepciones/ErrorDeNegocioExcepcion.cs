using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Logica.Excepciones
{
    public class ErrorDeNegocioExcepcion : MasterExcepcion
    {
        public ErrorDeNegocioExcepcion(string message)
        : base(message, 400) { }
    }
}
