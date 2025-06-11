using Azure.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hango.Logica.MercadoPago
{

    public interface IMercadoPagoLogica
    {
        Task<string> CrearLinkDePago(string accessToken, decimal monto, string descripcion, string email);
        Task<string> GenerarLinkAutorizacionMercadoPago(int idUsuario);
        Task<string> ObtenerAccessToken(string code, int idUsuario);
        Task<string> RealizarPagoDirecto(string payerAccessToken, string vendedorUserId, decimal monto, string emailPagador, string descripcion);
        
    }
    public class MercadoPagoLogica : IMercadoPagoLogica
    {
       
        string clientId = "8820474729966238";
        private readonly string clientSecret = "AC4V1f7PIbs5MdohDqZfMeS4NBO4U8Fo";
        

        public string ngrok = "https://ef87-2800-810-441-9bf7-81e1-ba81-d9d7-7ea8.ngrok-free.app";
        public MercadoPagoLogica()
        {    
        }

        public async Task<string> GenerarLinkAutorizacionMercadoPago(int idUsuario)
        {
            var redirectUri = $"{ngrok}/Pagos/callback";

            return $"https://auth.mercadopago.com.ar/authorization?client_id={clientId}&response_type=code&redirect_uri={redirectUri}&state={idUsuario}";
        }

        public async Task<string> CrearLinkDePago(string accessToken, decimal monto, string descripcion, string email)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var preference = new
            {
                items = new[]
                {
                new
                {
                    title = descripcion,
                    quantity = 1,
                    currency_id = "ARS",
                    unit_price = monto
                }
            },
                back_urls = new
                {
                    success = $"{ngrok}/Pagos/pago-exitoso",
                    failure = $"{ngrok}/Pagos/pago-fallido",
                    pending = $"{ngrok}/Pagos/pago-pendiente"
                },
                auto_return = "approved",
                payer = new
                {
                    email = email
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(preference), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.mercadopago.com/checkout/preferences", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception("Error al crear la preferencia de MercadoPago: " + body);

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("init_point").GetString();
        }



        public async Task<string> ObtenerAccessToken(string code, int idUsuario)
        {
            var redirectUri = $"{ngrok}/Pagos/callback";

            var values = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", redirectUri }
        };

            using var httpClient = new HttpClient();
            var content = new FormUrlEncodedContent(values);

            var response = await httpClient.PostAsync("https://api.mercadopago.com/oauth/token", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception("Error al obtener access token: " + responseString);

            using var jsonDoc = JsonDocument.Parse(responseString);
            var accessToken = jsonDoc.RootElement.GetProperty("access_token").GetString();

            return accessToken;
        }
        public async Task<string> RealizarPagoDirecto(string payerAccessToken, string vendedorUserId, decimal monto, string emailPagador, string descripcion)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payerAccessToken);
            var responses = await httpClient.GetAsync("https://api.mercadopago.com/users/me");
            var contentt = await responses.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(contentt);
            var payerId = jsonDoc.RootElement.GetProperty("id").GetInt64();
            var payment = new
            {
                transaction_amount = monto,
          //      payment_method_id = "account_money",
                description = descripcion,
                external_reference = vendedorUserId, 
             //   binary_mode = false,
                payer = new
                {
                    email = emailPagador,
                    id = payerId
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payment), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.mercadopago.com/v1/payments", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception("Error al realizar el pago directo: " + body);

            using var jsonDoc2 = JsonDocument.Parse(body);
            var idPago = jsonDoc2.RootElement.GetProperty("id").GetInt64();
            var status = jsonDoc2.RootElement.GetProperty("status").GetString();

            return $"Pago {idPago} realizado con estado: {status}";
        }
        
    }
}
