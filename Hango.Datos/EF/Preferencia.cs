using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class Preferencia
{
    public int IdPreferencia { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Grupo_Preferencia> Grupo_Preferencia { get; set; } = new List<Grupo_Preferencia>();

    public virtual ICollection<Usuario_Preferencia> Usuario_Preferencia { get; set; } = new List<Usuario_Preferencia>();
}
