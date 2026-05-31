using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace DatabaseManager.Models
{
    public partial class DatabaseNode : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private ObservableCollection<TableNode> _tables = new();
    }
}
