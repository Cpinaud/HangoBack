using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class Evento
{
    public int IdEvento { get; set; }

    public int GrupoId { get; set; }

    public int UsuarioId { get; set; }

    public DateTime Fecha { get; set; }

    public string TipoEvento { get; set; } = null!;

    public int? IdEventoRelacionado { get; set; }

    public string Contenido { get; set; } = null!;

    public virtual ICollection<EventoUsuario> EventoUsuario { get; set; } = new List<EventoUsuario>();

    public virtual ICollection<Notificacion> Notificacion { get; set; } = new List<Notificacion>();
}
