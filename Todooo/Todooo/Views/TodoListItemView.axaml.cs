using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Todooo.ViewModels;

namespace Todooo.Views;

public partial class TodoListItemView : UserControl
{
    public TodoListItemView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is TodoListItemViewModel vm)
        {
            _ = vm.TryLoadImageAsync();
        }
    }
}
