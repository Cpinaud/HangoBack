using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Logica.Registro
{
    public interface ICorreoLogica
    {
        Task EnviarCodigo(string destinatario, string codigoVerificacion, bool recuperacion);
        
    }
    public class CorreoLogica : ICorreoLogica
    {
        private readonly IConfiguration _configuration;

        public CorreoLogica(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarCodigo(string destinatario, string codigoVerificacion, bool recuperacion)
        {
            string smtpHost = _configuration["Correo:SmtpHost"];
            int smtpPort = int.Parse(_configuration["Correo:SmtpPort"]);
            string remitente = _configuration["Correo:Remitente"];
            string clave = _configuration["Correo:Clave"];

            string asunto;
            string cuerpo;

            if (recuperacion)
            {
                string linkRecuperacion = $"http://localhost:5222/Restablecer/solicitar-restablecimiento?c={codigoVerificacion}";
                asunto = "Recuperá tu contraseña";
                cuerpo = $"Hola, hacé clic en este enlace para restablecer tu contraseña: {linkRecuperacion}";
            }
            else
            {
                string linkVerificacion = $"http://localhost:5222/Registro/confirmar-cuenta?c={codigoVerificacion}";
                asunto = "Verificá tu cuenta";
                cuerpo = $"Hola, hacé clic en este link para verificar tu cuenta: {linkVerificacion}";
            }

            using var cliente = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(remitente, clave),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(remitente),
                Subject = asunto,
                Body = cuerpo,
                IsBodyHtml = false
            };

            mail.To.Add(destinatario);

            await cliente.SendMailAsync(mail);
        }


    }
}
