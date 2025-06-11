using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class Voto_Plan
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public int PlanId { get; set; }

    public int? Voto { get; set; }

    public DateTime FechaVoto { get; set; }

    public virtual Planes Plan { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
