using Hango.Datos.DTO.Grupo;
using Hango.Datos.EF;
using Hango.Repositorios.Grupo;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Repositorios.Zona
{
    public interface IZonaRepositorio
    {

        Task<Zonas> ObtenerZonaPorId(int idZona);
        Task GuardarZonaEnGrupo(Zonas_Grupos zonaGrupo);
    }

    public class ZonaRepositorio : IZonaRepositorio
    {
        private readonly HangoContext _context;
        public ZonaRepositorio(HangoContext context)
        {
            _context = context;

        }

        public async Task<Zonas> ObtenerZonaPorId(int idZona)
        {
            return await _context.Zonas.FirstOrDefaultAsync(z => z.Id == idZona);
        }

        public async Task GuardarZonaEnGrupo(Zonas_Grupos zonaGrupo)
        {
             _context.Zonas_Grupos.Add(zonaGrupo);
            await _context.SaveChangesAsync();

        }
    }

}
