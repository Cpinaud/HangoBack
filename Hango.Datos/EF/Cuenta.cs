using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class Cuenta
{
    public int IdCuenta { get; set; }

    public int IdPlan { get; set; }

    public int UsuarioCreadorId { get; set; }

    public string? Descripcion { get; set; }

    public decimal? MontoInicial { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public DateTime? FechaCierre { get; set; }

    public decimal? MontoTotal { get; set; }

    public int? UsuarioReceptorId { get; set; }

    public string? Estado { get; set; }

    public bool? ReceptorAceptaMercadoPago { get; set; }

    public virtual Planes IdPlanNavigation { get; set; } = null!;

    public virtual ICollection<ParticipacionCuenta> ParticipacionCuenta { get; set; } = new List<ParticipacionCuenta>();

    public virtual ICollection<TransferenciaSugerida> TransferenciaSugerida { get; set; } = new List<TransferenciaSugerida>();

    public virtual Usuario UsuarioCreador { get; set; } = null!;

    public virtual Usuario? UsuarioReceptor { get; set; }
}
