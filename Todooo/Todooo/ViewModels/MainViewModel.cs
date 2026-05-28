using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Todooo.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private string _mainTitleTxt = "Todoooo";
    public ObservableCollection<TodoListItemViewModel>? TodoListItem { get; } = new();
    [ObservableProperty] private TodoListItemViewModel? _selectTodoListItem;

    public MainViewModel()
    {
        for (int i = 0; i < 100000; i++)
        {
            TodoListItem?.Add(new TodoListItemViewModel()
            {
                Name = $"Name-{i}",
                Description = $"Description - - {i}"
            });
        }
    }
}