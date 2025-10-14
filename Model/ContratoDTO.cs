using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Imdeliceapp.Model
{

    public class ContratoDTO
    {
        public int piFolioContrato { get; set; }
        public string acNumeroContrato { get; set; }
        public DateTime? dtFechaContratacion { get; set; }
        public int fiFolioCliente { get; set; }
        public int? dsiTipoContrato { get; set; }
        public string ddTiieContratacion { get; set; }
        public int? fiFolioSolicitud { get; set; }
        public int dsiStatus { get; set; }
        public int? dbModoProtegido { get; set; }
        public string dsiMotivoCancelacion { get; set; }
        public int? dbFacturacionAutomatica { get; set; }
        public int? dbDomiciliacion { get; set; }
        public string dcUsuarioModif { get; set; }
        public DateTime? dtFechaModif { get; set; }
        public List<AnexoDTO> anexos { get; set; }
        
    }

    public class AnexoDTO
    {
        public int piFolioAnexo { get; set; }
        public int fiFolioContrato { get; set; }
        public string acNumeroAnexo { get; set; }
        public int? fiFolioCotizacion { get; set; }

        public int? fiFolioInventario { get; set; }
        public string dcAnexoSustituye { get; set; }
        public string dsiMotivoCancelacion { get; set; }
        public string dcUsuarioModif { get; set; }
        public DateTime? dtFechaModif { get; set; }
        public int? dsiStatus { get; set; }
        public int? dbFacturacionAutomatica { get; set; }
        public int? dbDepositoEntregado { get; set; }
        public DateTime? dtFechaDepositoEntregado { get; set; }
        public int? dbDomiciliacion { get; set; }

        public string avcBanco { get; set; }

        public InventarioDTO inventario { get; set; }
        public CotizacionDTO cotizacion { get; set; }

    }
    public class InventarioDTO
{
    public int? piFolioInventario { get; set; }
    public int? dsiTipoEquipo { get; set; }
    public string dvcClave { get; set; }
    public string dvcClaveDescripcion { get; set; }
    public int? fiFolioProveedor { get; set; }
    public string dvcMarca { get; set; }
    public string dvcTipo { get; set; }
    public int? dsiModelo { get; set; }
    public string dvcColor { get; set; }
    public string dvcSerie { get; set; }
    public string dvcMotor { get; set; }
    public string avcPlacas { get; set; }
    public string dmSubtotal { get; set; }
    public string dmIVA { get; set; }
    public string dmTotal { get; set; }
    public string dvcFactor { get; set; }
    public string ddTasaIVA { get; set; }
    public int? dbCambioPropietario { get; set; }
    public int? fiEstadoPlacas { get; set; }
    public string dcUsuarioModif { get; set; }
    public string dtFechaModif { get; set; }
}

public class CotizacionDTO
{
    public int? piFolioCotizacion { get; set; }
    public string acNumeroCotizacion { get; set; }
    public string dtFechaCotizacion { get; set; }
    public string dcTrato { get; set; }
    public string dvcSolicitante { get; set; }
    public string dvcPuesto { get; set; }
    public int? fiFolioCliente { get; set; } //

    public string dvcNombreEmpresa { get; set; }
    public int? dsiTipoOperacion { get; set; }
    public string dvcElaboro { get; set; }
    public int? dsiTipoRenta { get; set; }
    public int? dsiTipoEquipo { get; set; }
    public string dvcDescripcionEquipo { get; set; }
    public int? dsiPlazoOperacion { get; set; }
    public string ddFactorArrendamiento { get; set; }
    public string ddTasaInteres { get; set; }
    public string dmValorConIva { get; set; }
    public string ddPctjeIvaAplicado { get; set; }
    public string dmValorSinIva { get; set; }
    public string ddPctjeOpcionCompra { get; set; }
    public string dmValorResidual { get; set; }
    public string ddPctjeGastos { get; set; }
    public string dmGastos { get; set; }
    public string ddPctjeHonorarios { get; set; }
    public string dmHonorarios { get; set; }
    public string ddMesesDeposito { get; set; }
    public string dmDeposito { get; set; }
    public string dmOtrosCargos { get; set; }
    public string dmBonificacion { get; set; }
    public string dmCuotaMensual { get; set; }
    public string dmEnganche { get; set; }
    public string dmSaldoFinanciado { get; set; }
    public string dmSaldoInsoluto { get; set; }
    public int? dsiNumeroPagos { get; set; }
    public int? dsiPagoActual { get; set; }
    public string dmContrapunto { get; set; }
    public string dmPlacas { get; set; }
    public string dmTenencia { get; set; }
    public string dmGestoria { get; set; }
    public string dmSaldoFavor { get; set; }
    public string ddTasaReal { get; set; }
    public string dmValorReal { get; set; }
    public string dcUsuarioModif { get; set; }
    public string dtFechaModif { get; set; }
    public object? dTimeStamp { get; set; } // ✅ compatible con cualquier forma


}
    public class ApiResponse<T>
    {
        public string message { get; set; }
        public T data { get; set; }
    public object error { get; set; }
}

}