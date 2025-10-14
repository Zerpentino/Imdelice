// Model/CategoryGroup.cs
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Imdeliceapp.Model
{
    public class CategoryGroup : INotifyPropertyChanged
    {
        string _id = "";
        string _name = "";
        bool _isExpanded;
        ObservableCollection<MenuRow> _items = new();

        public string Id { get => _id; set { _id = value; OnPropertyChanged(); } }
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public bool IsExpanded { get => _isExpanded; set { _isExpanded = value; OnPropertyChanged(); } }
        public ObservableCollection<MenuRow> Items { get => _items; set { _items = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class MenuRow
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
    }
}
