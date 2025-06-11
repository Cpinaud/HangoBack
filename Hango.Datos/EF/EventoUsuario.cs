using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class EventoUsuario
{
    public int Id { get; set; }

    public int EventoId { get; set; }

    public int UsuarioId { get; set; }

    public bool Leido { get; set; }

    public virtual Evento Evento { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
