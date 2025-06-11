using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class Presupuesto
{
    public int IdPresupuesto { get; set; }

    public decimal Estimado { get; set; }

    public byte Rango { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public int IdUsuario { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
