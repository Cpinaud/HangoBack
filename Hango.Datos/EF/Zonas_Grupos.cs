using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class Zonas_Grupos
{
    public int Id { get; set; }

    public int IdZona { get; set; }

    public int IdGrupo { get; set; }

    public virtual Grupo IdGrupoNavigation { get; set; } = null!;

    public virtual Zonas IdZonaNavigation { get; set; } = null!;
}
