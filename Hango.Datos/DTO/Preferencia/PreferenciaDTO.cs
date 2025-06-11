namespace Hango.Datos.DTO.Preferencia
{
    public class PreferenciaDTO
    {
        public int IdPreferencia { get; set; }

        public string Nombre { get; set; } = null!;

        public int Prioridad { get; set; }
    }
}