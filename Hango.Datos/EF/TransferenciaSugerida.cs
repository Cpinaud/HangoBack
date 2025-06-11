using System;
using System.Collections.Generic;

namespace Hango.Datos.EF;

public partial class TransferenciaSugerida
{
    public int IdTransferencia { get; set; }

    public int CuentaId { get; set; }

    public int DeUsuarioId { get; set; }

    public int AUsuarioId { get; set; }

    public decimal Monto { get; set; }

    public virtual Usuario AUsuario { get; set; } = null!;

    public virtual Cuenta Cuenta { get; set; } = null!;

    public virtual Usuario DeUsuario { get; set; } = null!;
}
