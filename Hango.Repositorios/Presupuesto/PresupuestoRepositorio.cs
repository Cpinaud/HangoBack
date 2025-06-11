using Hango.Datos.EF;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Repositorios.Presupuesto
{
    public interface IPresupuestoRepositorio
    {
        Task GuardarPresupuestoAsync(Hango.Datos.EF.Presupuesto presupuesto);
        Task ActualizarPresupuestoAsync(Hango.Datos.EF.Usuario usuario, byte? rango);
    }
    public class PresupuestoRepositorio : IPresupuestoRepositorio
    {
        private readonly HangoContext _context;

        public PresupuestoRepositorio(HangoContext context)
        {
            _context = context;
        }

        public async Task GuardarPresupuestoAsync(Hango.Datos.EF.Presupuesto presupuesto)
        {
            await _context.Presupuesto.AddAsync(presupuesto);
            await _context.SaveChangesAsync();
        }
        public async Task ActualizarPresupuestoAsync(Hango.Datos.EF.Usuario usuario, byte? rango)
        {
            var presupuesto = usuario.Presupuesto.FirstOrDefault();
            if (presupuesto != null)
            {
                presupuesto.Rango = (byte)rango;
            }
            else
            {
                usuario.Presupuesto.Add(new Hango.Datos.EF.Presupuesto
                {
                    IdUsuario = usuario.IdUsuario,
                    Rango = (byte)rango
                });
            }
        }
    }
}
