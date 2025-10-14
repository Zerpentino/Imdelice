namespace Imdeliceapp.Model
{
    public class PagoStripeRequest
    {
        public string paymentMethodId { get; set; }
        public decimal dmMonto { get; set; }
        public int piFolioPago { get; set; }
        public string dvcConcepto { get; set; }
        public string customerEmail { get; set; }
        public int fiFolioRecibo { get; set; }
        public int fiFolioDocumento { get; set; }
        public int fiFolioBanco { get; set; }
    }
}
