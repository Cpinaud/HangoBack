using Hango.Datos.EF;
using Hango.Repositorios.Presupuesto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Logica.Presupuesto
{
    public interface IPresupuestoLogica
    {
        Task GuardarPresupuestoAsync(Hango.Datos.EF.Presupuesto presupuesto);
        Task ActualizarPresupuestoAsync(Hango.Datos.EF.Usuario usuario, byte? rango);
    }
    public class PresupuestoLogica : IPresupuestoLogica
    {
        private readonly IPresupuestoRepositorio _presupuestoRespositorio;

        public PresupuestoLogica(IPresupuestoRepositorio presupuestoRepositorio)
        {
            _presupuestoRespositorio = presupuestoRepositorio;
        }

        public async Task ActualizarPresupuestoAsync(Datos.EF.Usuario usuario, byte? rango)
        {
            await _presupuestoRespositorio.ActualizarPresupuestoAsync(usuario, rango);
        }

        public async Task GuardarPresupuestoAsync(Datos.EF.Presupuesto presupuesto)
        {
            await _presupuestoRespositorio.GuardarPresupuestoAsync(presupuesto);
        }
    }
}
