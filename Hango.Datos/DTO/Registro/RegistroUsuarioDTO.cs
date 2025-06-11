using System.ComponentModel.DataAnnotations;

namespace Hango.Datos.DTO.Registro
{
    public class RegistroUsuarioDTO
    {
        public int IdUsuario { get; set; }

        [Required]
        public string Nombre { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = null!;

        [Required]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string RepetirPassword { get; set; }

        public int? IdAvatar { get; set; }

        public bool? HorariosPrivados { get; set; }

        public DateTime? CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }
    }
}
