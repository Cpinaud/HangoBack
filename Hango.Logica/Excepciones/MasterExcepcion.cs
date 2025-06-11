using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Logica.Excepciones
{
    public abstract class MasterExcepcion : Exception
    {

        public int StatusCode { get; }

        protected MasterExcepcion(string message, int statusCode = 400) : base(message)
        {
            StatusCode = statusCode;
        }
        protected MasterExcepcion(string message, int statusCode, Exception innerException)
       : base(message, innerException)
        {
            StatusCode = statusCode;
        }

    }
}
