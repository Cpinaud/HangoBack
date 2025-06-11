using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class HorarioDisponible
{
    public int IdHorarioDisponible { get; set; }

    public int IdUsuario { get; set; }

    public DateTime Fecha { get; set; }

    public byte DiaSemana { get; set; }

    public TimeSpan HorarioInicio { get; set; }

    public TimeSpan HorarioFin { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
