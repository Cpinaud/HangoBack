using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class Planes
{
    public int Id { get; set; }

    public int PropuestaId { get; set; }

    public int DiaSemana { get; set; }

    public DateOnly Fecha { get; set; }

    public int PreferenciaId { get; set; }

    public TimeSpan Hora { get; set; }

    public string? Lugar { get; set; }

    public string? Descripcion { get; set; }

    public string? Direccion { get; set; }

    public string? Estado { get; set; }

    public string? Presupuesto { get; set; }

    public virtual ICollection<Cuenta> Cuenta { get; set; } = new List<Cuenta>();

    public virtual Propuesta Propuesta { get; set; } = null!;

    public virtual ICollection<Usuario_Plan> Usuario_Plan { get; set; } = new List<Usuario_Plan>();

    public virtual ICollection<Voto_Plan> Voto_Plan { get; set; } = new List<Voto_Plan>();
}
