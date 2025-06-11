using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class RefreshToken
{
    public int Id { get; set; }

    public int IdUsuario { get; set; }

    public string Token { get; set; } = null!;

    public DateTime HechoToken { get; set; }

    public DateTime ExpiroToken { get; set; }

    public DateTime? RevocarToken { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
