using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class Grupo
{
    public int IdGrupo { get; set; }

    public string Nombre { get; set; } = null!;

    public string UrlImagen { get; set; } = null!;

    public bool Activo { get; set; }

    public string TokenInvitacion { get; set; } = null!;

    public DateTime? CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual ICollection<Grupo_Preferencia> Grupo_Preferencia { get; set; } = new List<Grupo_Preferencia>();

    public virtual ICollection<Propuesta> Propuesta { get; set; } = new List<Propuesta>();

    public virtual ICollection<Usuario_Grupo> Usuario_Grupo { get; set; } = new List<Usuario_Grupo>();

    public virtual ICollection<Zonas_Grupos> Zonas_Grupos { get; set; } = new List<Zonas_Grupos>();
}
