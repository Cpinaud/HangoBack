using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class Avatar
{
    public int IdAvatar { get; set; }

    public string Nombre { get; set; } = null!;

    public string RutaImagen { get; set; } = null!;
}
