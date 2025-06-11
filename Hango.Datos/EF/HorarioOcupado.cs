using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class HorarioOcupado
{
    public int IdHorarioOcupado { get; set; }

    public int IdUsuario { get; set; }

    public DateTime Fecha { get; set; }

    public byte DiaSemana { get; set; }

    public DateTime HorarioInicio { get; set; }

    public DateTime HorarioFin { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
