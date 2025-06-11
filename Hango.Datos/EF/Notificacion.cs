using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class Notificacion
{
    public int IdNotificacion { get; set; }

    public int UsuarioId { get; set; }

    public int EventoId { get; set; }

    public DateTime FechaEnvio { get; set; }

    public bool Leida { get; set; }

    public virtual Evento Evento { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
