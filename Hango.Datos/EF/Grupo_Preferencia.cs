using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class Grupo_Preferencia
{
    public int Id { get; set; }

    public int IdGrupo { get; set; }

    public int IdPreferencia { get; set; }

    public int? Prioridad { get; set; }

    public virtual Grupo IdGrupoNavigation { get; set; } = null!;

    public virtual Preferencia IdPreferenciaNavigation { get; set; } = null!;
}
