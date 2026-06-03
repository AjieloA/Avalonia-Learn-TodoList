using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Todooo.Helpers;

namespace Todooo.ViewModels;

public partial class TodoListItemViewModel : ViewModelBase
{
    [ObservableProperty] private string? _name;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private string? _imageUrl;
    [ObservableProperty] private Bitmap? _image;

    private int _imageLoaded;

    public async Task TryLoadImageAsync(CancellationToken ct = default)
    {
        // 只加载一次；Interlocked 保证多线程安全
        if (Interlocked.Exchange(ref _imageLoaded, 1) == 1)
            return;

        var url = ImageUrl;
        if (string.IsNullOrEmpty(url)) return;

        try
        {
            Image = await ImageCache.GetOrDownloadAsync(url, ct);
        }
        catch (OperationCanceledException)
        {
            // 取消时复位标记，下次 attach 可重新加载
            _imageLoaded = 0;
        }
        catch
        {
            // 加载失败时保持 Image 为 null
        }
    }
}
