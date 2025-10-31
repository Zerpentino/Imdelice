namespace Imdeliceapp.Models;

public class ApiEnvelopeMods<T>
{
    public string? error { get; set; }
    public T? data { get; set; }
    public string? message { get; set; }
}

public class ModifierOptionDTO
{
    public int id { get; set; }
    public int groupId { get; set; }
    public string name { get; set; } = "";
    public int priceExtraCents { get; set; }
    public bool isDefault { get; set; }
    public bool isActive { get; set; }
    public int position { get; set; }
}

public class ModifierGroupDTO
{
    public int id { get; set; }
    public string name { get; set; } = "";
    public string? description { get; set; }
    public int minSelect { get; set; }
    public int? maxSelect { get; set; }
    public bool isRequired { get; set; }
    public bool isActive { get; set; }
    public int position { get; set; }
    public int? appliesToCategoryId { get; set; }
    public List<ModifierOptionDTO> options { get; set; } = new();
}

public class ProductGroupLinkDTO
{
    public int id { get; set; }
    public int position { get; set; }
    public ModifierGroupDTO? group { get; set; }
}

public class VariantModifierGroupLinkDTO
{
    public int groupId { get; set; }
    public ModifierGroupDTO? group { get; set; }
    public int minSelect { get; set; }
    public int? maxSelect { get; set; }
    public bool isRequired { get; set; }
    public bool? inheritsFromProduct { get; set; } // nullable for backwards compatibility
}
