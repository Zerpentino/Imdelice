namespace Imdeliceapp.Models;

public class TableDTO
{
    public int id { get; set; }
    public string? name { get; set; }
    public int seats { get; set; }
    public bool isActive { get; set; }
}

public class TableListItemDTO : TableDTO
{
}

public record TableInput(string name, int seats, bool? isActive = null);
