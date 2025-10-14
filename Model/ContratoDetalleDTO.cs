namespace Imdeliceapp.Model
{
    public class ContratoDetalleDTO
    {
        public string? dvcTipo { get; set; }
        public string? dvcMarca { get; set; }
        public int? dsiModelo { get; set; }
        public string? dvcSerie { get; set; }
        public string? dvcMotor { get; set; }
        public string? dvcColor { get; set; }
        public string? dvcPlacas { get; set; }

        public string? dvcPoliza { get; set; }
        public string? dvcCompania { get; set; }
        public string? dvcCobertura { get; set; }
        public DateTime? dtInicioPoliza { get; set; }
        public DateTime? dtFinPoliza { get; set; }
        public decimal? dsmPrima { get; set; }
        public string? dvcPeriodo { get; set; }

        public DateTime? dtFechaContratacion { get; set; }
        public DateTime? dtFechaVencimiento { get; set; }
        public string? dvcDiaVencimiento { get; set; }
        public decimal? dsmRentaMensual { get; set; }
        public decimal? dsmOtrosCargos { get; set; }
        public decimal? dsmValorResidual { get; set; }

        // Si en el futuro recibes una lista de recibos:
        public List<ReciboDTO>? recibos { get; set; }
    }

    public class ReciboDTO
    {
        public int id { get; set; }
        public int? aiNumRecibo { get; set; }
        public DateTime? fechaVencimiento { get; set; }
        public DateTime? fechaPago { get; set; }
        public string? subtotal { get; set; }
        public string? iva { get; set; }

        public string? total { get; set; }
        public string? saldo { get; set; }
        public string? status { get; set; }

        public string? tipo { get; set; }
        public string? subtipo { get; set; }
        public string? servicios { get; set; }
        

}


    public class ApiContratoResponse
{
    public string? message { get; set; }
    public List<ContratoData>? data { get; set; }
}

public class ContratoData
{
    public string? acNumeroContrato { get; set; }
    public DateTime? dtFechaContratacion { get; set; } // ✅ nullable

    public List<AnexoData>? anexos { get; set; }
}

public class AnexoData
{
    public int? piFolioAnexo { get; set; } 
    public int? dsiStatus { get; set; }
    public string? acNumeroAnexo { get; set; }
    public InventarioData? inventario { get; set; }
    public CotizacionData? cotizacion { get; set; }
}


public class InventarioData
{
    public string? dvcMarca { get; set; }
    public string? dvcTipo { get; set; }
    public int? dsiModelo { get; set; }
    public string? dvcSerie { get; set; }
    public string? dvcMotor { get; set; }
    public string? dvcColor { get; set; }
    public string? avcPlacas { get; set; }
}

public class CotizacionData
{
    public string? dmCuotaMensual { get; set; }
    public string? dmValorResidual { get; set; }
}

}
