using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class TipoParticipacion
{
    public int IdTipoParticipacion { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<ParticipacionCuenta> ParticipacionCuenta { get; set; } = new List<ParticipacionCuenta>();
}
