using Hango.Datos.EF;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Repositorios.Utilidades
{
    public interface IUnionDeTarea
    {
        Task GuardarCambiosAsync();
    }
    public class UnionDeTarea: IUnionDeTarea
    {
        private readonly HangoContext _context;

        public UnionDeTarea(HangoContext context)
        {
            _context = context;
        }

        
        public Task GuardarCambiosAsync() => _context.SaveChangesAsync();
    
    }
}
