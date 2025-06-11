using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class Usuario_Propuesta
{
    public int IdUsuario { get; set; }
    public int IdPropuesta { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
    public virtual Propuesta Propuesta { get; set; } = null!;
}
