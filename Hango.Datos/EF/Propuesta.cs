using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class Propuesta
{
    public int IdPropuesta { get; set; }

    public int GrupoId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaVencimiento { get; set; }

    public string Origen { get; set; } = null!;

    public int? IdEvento { get; set; }

    public virtual Evento Evento { get; set; } = null!;

    public virtual Grupo Grupo { get; set; } = null!;

    public virtual ICollection<Planes> Planes { get; set; } = new List<Planes>();

    public virtual ICollection<Usuario_Propuesta> Usuario_Propuesta { get; set; } = new List<Usuario_Propuesta>();
}
