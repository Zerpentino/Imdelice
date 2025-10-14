// Imdeliceapp/Models/CategoryDTO.cs
namespace Imdeliceapp.Models
{
    public class CategoryDTO
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? slug { get; set; }
        public int position { get; set; }
        public bool isActive { get; set; }
        public bool isComboOnly { get; set; }
        public int? parentId { get; set; }
    }
}
