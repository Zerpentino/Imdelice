namespace Imdeliceapp.Models;

public class ProductLiteDTO
{
    public int id { get; set; }
    public string? name { get; set; }
    public string? type { get; set; }          // SIMPLE | VARIANTED | COMBO
    public int? priceCents { get; set; }
    public bool isActive { get; set; }
    public int categoryId { get; set; }
}

public class GroupProductLinkDTO
{
    public int linkId { get; set; }
    public int position { get; set; }
    public ProductLiteDTO product { get; set; } = new();
}
