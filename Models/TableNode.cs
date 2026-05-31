using CommunityToolkit.Mvvm.ComponentModel;

namespace DatabaseManager.Models
{
    public partial class TableNode : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;
    }
}
