using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Todooo.ViewModels;

namespace Todooo.Views;

public partial class TodoListItemView : UserControl
{
    private CancellationTokenSource? _cts;

    public TodoListItemView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is TodoListItemViewModel vm)
        {
            _cts = new CancellationTokenSource();
            _ = vm.TryLoadImageAsync(_cts.Token);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
