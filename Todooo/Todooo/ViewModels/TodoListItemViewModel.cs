using CommunityToolkit.Mvvm.ComponentModel;

namespace Todooo.ViewModels;

public partial class TodoListItemViewModel : ViewModelBase
{
    [ObservableProperty] private string? _name;
    [ObservableProperty] private string? _description;
}