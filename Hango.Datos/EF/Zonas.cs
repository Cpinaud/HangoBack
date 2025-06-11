using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class Zonas
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Zonas_Grupos> Zonas_Grupos { get; set; } = new List<Zonas_Grupos>();
}
