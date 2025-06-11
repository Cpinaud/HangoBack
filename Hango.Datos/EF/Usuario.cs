using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public string Nombre { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password_Hash { get; set; } = null!;

    public int IdAvatar { get; set; }

    public bool? HorariosPrivados { get; set; }

    public bool? EstaVerificado { get; set; }

    public string? CodigoVerificacion { get; set; }

    public DateTime? FechaVerificacion { get; set; }

    public DateTime? CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }
    public string? AccessTokenMercadoPago { get; set; }

    public virtual ICollection<Cuenta> CuentaUsuarioCreador { get; set; } = new List<Cuenta>();

    public virtual ICollection<Cuenta> CuentaUsuarioReceptor { get; set; } = new List<Cuenta>();

    public virtual ICollection<EventoUsuario> EventoUsuario { get; set; } = new List<EventoUsuario>();

    public virtual ICollection<ExcepcionLibre> ExcepcionLibre { get; set; } = new List<ExcepcionLibre>();

    public virtual ICollection<ExcepcionOcupado> ExcepcionOcupado { get; set; } = new List<ExcepcionOcupado>();

    public virtual ICollection<HorarioDisponible> HorarioDisponible { get; set; } = new List<HorarioDisponible>();

    public virtual ICollection<Notificacion> Notificacion { get; set; } = new List<Notificacion>();

    public virtual ICollection<ParticipacionCuenta> ParticipacionCuenta { get; set; } = new List<ParticipacionCuenta>();

    public virtual ICollection<Presupuesto> Presupuesto { get; set; } = new List<Presupuesto>();

    public virtual ICollection<RefreshToken> RefreshToken { get; set; } = new List<RefreshToken>();

    public virtual ICollection<TransferenciaSugerida> TransferenciaSugeridaAUsuario { get; set; } = new List<TransferenciaSugerida>();

    public virtual ICollection<TransferenciaSugerida> TransferenciaSugeridaDeUsuario { get; set; } = new List<TransferenciaSugerida>();

    public virtual ICollection<Usuario_Grupo> Usuario_Grupo { get; set; } = new List<Usuario_Grupo>();

    public virtual ICollection<Usuario_Plan> Usuario_Plan { get; set; } = new List<Usuario_Plan>();

    public virtual ICollection<Usuario_Preferencia> Usuario_Preferencia { get; set; } = new List<Usuario_Preferencia>();

    public virtual ICollection<Voto_Plan> Voto_Plan { get; set; } = new List<Voto_Plan>();

    public virtual ICollection<Usuario_Propuesta> Usuario_Propuesta { get; set; } = new List<Usuario_Propuesta>();
}
