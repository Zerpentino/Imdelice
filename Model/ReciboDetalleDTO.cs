using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Imdeliceapp.Model
{
    public class ReciboDetalleDTO
{
    public int      id          { get; set; }
    public DateTime fechaPago   { get; set; }
    public decimal  subtotal    { get; set; }
    public decimal  iva         { get; set; }
    public decimal  servicios   { get; set; }
    public decimal  abonado     { get; set; }
    public decimal total { get; set; }
    public string   concepto    { get; set; } = "";
    public string   referencia  { get; set; } = "";
}

}