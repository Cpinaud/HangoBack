using Microsoft.AspNetCore.SignalR;

namespace Hango.Hubs
{
    public class GrupoHub:Hub
    {
        public async Task UnirseAlGrupo(string idGrupo)
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, idGrupo);
                Console.WriteLine($"Conexión {Context.ConnectionId} se unió al grupo {idGrupo}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error en UnirseAlGrupo: " + ex.Message);
                throw; 
            }
        }

        public async Task NotificarCambioDeIntegrantes(string idGrupo)
        {
            await Clients.Group(idGrupo).SendAsync("IntegrantesActualizados", idGrupo);
            Console.WriteLine($"📢 Se notificó a los miembros del grupo {idGrupo} que hubo un cambio en los integrantes.");
        }

    }
}
