using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class Usuario_Grupo
{
    public int Id { get; set; }

    public int IdUsuario { get; set; }

    public int IdGrupo { get; set; }

    public bool Administrador { get; set; }

    public bool? Favorito { get; set; }

    public DateTime? Ingreso { get; set; }

    public virtual Grupo IdGrupoNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
