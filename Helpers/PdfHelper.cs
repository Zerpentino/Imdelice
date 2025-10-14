using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using Imdeliceapp.Model;
using Microsoft.Maui.Storage;
using System.Globalization;
using Imdeliceapp.Pages;
// justo debajo de los demás using
using MauiColors  = Microsoft.Maui.Graphics.Colors;
using PdfColors   = QuestPDF.Helpers.Colors;


namespace Imdeliceapp.Helpers;

public static class PdfHelper
{
// Helpers/PdfHelper.cs  (arriba del todo)
static PdfHelper()
{
    QuestPDF.Settings.License      = LicenseType.Community;
    QuestPDF.Settings.EnableCaching = true;
}


    static readonly CultureInfo _mx = new("es-MX");

    public static async Task<string> GenerarReciboAsync(ReciboDetalleDTO r)
    {
        
        var ruta = Path.Combine(FileSystem.CacheDirectory, $"Recibo_{r.id}.pdf");

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Size(PageSizes.A4);
                page.PageColor(PdfColors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Content().Column(col =>
                {
                    // ————— helper local —————
                    void fila(string label, string value, bool bold = false)
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text(label).Bold();
                            var txt = row.ConstantItem(150)
                                         .AlignRight()
                                         .Text(value);
                            if (bold) txt.Bold();
                        });
                    }

                    // --- Encabezado empresa ----------
                    col.Item().AlignCenter().Text("Arrendamiento Global S.A. de C.V.")
                               .FontSize(18).Bold();
                    col.Item().AlignCenter().Text("RFC: AGS123456789").FontSize(10);
                    col.Item().PaddingVertical(10).LineHorizontal(1);

                    // --- Folio / Fecha ---------------
                    fila("Folio:",  r.id.ToString());
                    fila("Fecha:",  r.fechaPago.ToString("dd/MM/yyyy", _mx));

                    // --- Concepto --------------------
                    col.Item().PaddingTop(10).Text("Concepto:").Bold();
                    col.Item().Text(r.concepto);

                    // --- Importes --------------------
                    col.Item().PaddingTop(10);
                    fila("Subtotal:",  r.subtotal.ToString("C2", _mx));
                    fila("IVA:",       r.iva.ToString("C2", _mx));
                    fila("Servicios:", r.servicios.ToString("C2", _mx));
                    fila("Abono:",     r.abonado.ToString("C2", _mx));
                    col.Item().LineHorizontal(1);
                    fila("TOTAL:",     r.total.ToString("C2", _mx), true);

                    // --- Método / referencia ---------
                    col.Item().PaddingTop(10).Text("Método de pago: Stripe Checkout");
                    col.Item().PaddingTop(5).Text("Referencia:");
                    col.Item().Text(r.referencia)          // enlace / token
          .FontSize(9)
          .FontColor(PdfColors.Blue.Medium); 
                });
            });
        })
        .GeneratePdf(ruta);

        return ruta;
    }
}