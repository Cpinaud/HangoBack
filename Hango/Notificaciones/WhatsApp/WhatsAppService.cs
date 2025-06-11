using System.Net.Http;
using System.Threading.Tasks;

namespace Hango.Notificaciones.WhatsApp
{
    public class WhatsAppService
    {
        private readonly HttpClient _httpClient;
        private const string InstanceId = "instance123320";
        private const string Token = "eyrajyarisj8pkmz";

        public WhatsAppService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task EnviarMensajeAsync(string numeroDestino, string mensaje)
        {
            var url = $"https://api.ultramsg.com/{InstanceId}/messages/chat" +
                      $"?token={Token}" +
                      $"&to="+549+"{numeroDestino}" +
                      $"&body={Uri.EscapeDataString(mensaje)}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode(); // Lanza excepción si algo falla
        }
    }
}