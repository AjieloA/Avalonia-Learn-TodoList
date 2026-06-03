using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace Todooo.Helpers;

public static class ImageCache
{
    private const int MaxRetries = 3;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMilliseconds(500);

    private static readonly HttpClient _http = new();
    private static readonly ConcurrentDictionary<string, WeakReference<Bitmap>> _cache = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public static async Task<Bitmap?> GetOrDownloadAsync(string url)
    {
        // 1. 缓存命中 → 直接返回
        if (_cache.TryGetValue(url, out var weakRef) && weakRef.TryGetTarget(out var cached))
            return cached;

        // 2. 获取或创建 per-URL 信号量
        var sem = _locks.GetOrAdd(url, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync();
        try
        {
            // 双重检查：等锁期间可能已被其他线程下载并缓存
            if (_cache.TryGetValue(url, out weakRef) && weakRef.TryGetTarget(out cached))
                return cached;

            // 3. 指数退避重试循环
            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    Console.WriteLine($"开始下载图片 Url：{url}");
                    var data = await _http.GetByteArrayAsync(url);
                    var bitmap = new Bitmap(new System.IO.MemoryStream(data));
                    _cache[url] = new WeakReference<Bitmap>(bitmap);
                    return bitmap;
                }
                catch (HttpRequestException) when (attempt < MaxRetries)
                {
                    // 网络错误，指数退避 + 随机抖动
                    var delay = InitialDelay * (1 << attempt) // 500ms → 1s → 2s
                                + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 200));
                    await Task.Delay(delay);
                }
                catch
                {
                    // 不可重试异常：直接放弃
                    return null;
                }
            }

            return null; // 重试用尽
        }
        finally
        {
            sem.Release();
        }
    }
}