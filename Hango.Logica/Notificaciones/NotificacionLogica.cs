using Hango.Datos.EF;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Logica.Notificaciones
{
    public interface INotificacionLogica
    {
        Task<bool> MarcarNotificacionComoLeida(int idNotificacion, int usuarioId);
    }
    public class NotificacionLogica : INotificacionLogica
    {
        private readonly HangoContext _context;
        public NotificacionLogica(HangoContext context)
        {
            _context = context;
        }

        public async Task<bool> MarcarNotificacionComoLeida(int idNotificacion, int usuarioId)
        {
           /* var evento = await _context.Notificacion
            .FirstOrDefaultAsync(e => e.Id == idNotificacion && e.IdUsuario == usuarioId);
            if (evento == null || evento.Leido)
            {
                return false; // Evento no encontrado o ya marcado como leído
            }*/
            return true;
        }
    }
}
