using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Todooo.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private string _mainTitleTxt = "Todoooo";
    public ObservableCollection<TodoListItemViewModel>? TodoListItem { get; } = new();
    [ObservableProperty] private TodoListItemViewModel? _selectTodoListItem;

    private Dictionary<int, string> imgDic = new Dictionary<int, string>()
    {
        { 1, "https://picture-new.88dog.com/g5ty9mai1699269161.png" },
        { 2, "https://picture-new.88dog.com/i79fydky1699269214.png" },
        { 3, "https://picture-new.88dog.com/yabypiac1743563877.png" },
        { 4, "https://picture-new.88dog.com/x615b0rt1699269295.png" },
        { 5, "https://picture-new.88dog.com/kw36kgsy1699268926.png" },
        { 6, "https://picture-new.88dog.com/anc2a12c1699269136.png" },
    };

    public MainViewModel()
    {
        var _random = new Random();
        for (int i = 0; i < 100000; i++)
        {
            var _idx = _random.Next(1, 7);
            if (!imgDic.TryGetValue(_idx, out string _url))
                _url = "https://picture-new.88dog.com/avatar.png";
            TodoListItem?.Add(new TodoListItemViewModel()
            {
                Name = $"Name-{i}",
                Description = $"Description - - {i}",
                ImageUrl = _url
            });
        }
    }
}