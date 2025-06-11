using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class ParticipacionCuenta
{
    public int IdParticipacion { get; set; }

    public int CuentaId { get; set; }

    public int UsuarioId { get; set; }

    public string Estado { get; set; } = null!;

    public decimal? MontoGastado { get; set; }

    public decimal MontoDebe { get; set; }

    public int IdTipoParticipacion { get; set; }

    public virtual Cuenta Cuenta { get; set; } = null!;

    public virtual TipoParticipacion IdTipoParticipacionNavigation { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
