using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class Usuario_Preferencia
{
    public int Id { get; set; }

    public int IdUsuario { get; set; }

    public int IdPreferencia { get; set; }

    public virtual Preferencia IdPreferenciaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
