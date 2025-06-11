using Hango.Datos.EF;
using Microsoft.EntityFrameworkCore;



namespace Hango.Logica.Zona
{
    public interface IZonasLogica
    {
        Task<string> ObtenerNombreZonaPorId(int idZona);
        Task<List<Zonas>> ObtenerZonas();
        Task<List<Zonas>> ObtenerZonasPorGrupo(int idGrupo);
    }
    public class ZonasLogica : IZonasLogica
    {
        private readonly HangoContext _context;

        public ZonasLogica(HangoContext context)
        {
            _context = context;
        }

        public async Task<string> ObtenerNombreZonaPorId(int idZona)
        {
            return await _context.Zonas
               .Where(z => z.Id == idZona)
               .Select(z => z.Nombre)
               .FirstOrDefaultAsync();
        }

        public async Task<List<Zonas>> ObtenerZonas()
        {
            return await _context.Zonas.ToListAsync();
        }

        public async Task<List<Zonas>> ObtenerZonasPorGrupo(int idGrupo)
        {
            return await _context.Zonas_Grupos
                .Where(zg => zg.IdGrupo == idGrupo)
                .Include(zg => zg.IdZonaNavigation)
                .Select(zg => zg.IdZonaNavigation)
                .ToListAsync();
        }
    }
}
