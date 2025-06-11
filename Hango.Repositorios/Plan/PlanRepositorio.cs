using Hango.Datos.EF;
using Hango.Repositorios.Utilidades;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Repositorios.Plan
{
    public interface IPlanRepositorio
    {
        Task<List<int>> ObtenerUsuariosIncluidosEnUnPlan(int idPlan);
    }
    public class PlanRepositorio : IPlanRepositorio
    {
        private readonly HangoContext _context;

        public PlanRepositorio(HangoContext context)
        {
            _context = context;
        }

        public async Task<List<int>> ObtenerUsuariosIncluidosEnUnPlan(int idPlan)
        {
            return await RepositoryHelper.EjecutarConManejoErroresAsync(async () =>
            {
                return await _context.Usuario_Plan
                                .Where(up => up.PlanId == idPlan && up.Estado == "INCLUIDO")
                                .Select(up => up.UsuarioId)
                                .Distinct()
                                .ToListAsync();
            });
        }
    }
}
