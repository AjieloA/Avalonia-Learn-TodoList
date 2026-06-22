using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using Todooo.Services;

namespace Todooo.Android;

[Activity(
    Label = "Todooo.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // Avalonia 的共享层不直接引用 Android SDK；启动 Android Activity 时把平台实现注入到共享服务入口。
        // MainViewModel 创建后会通过 PlatformServices.WechatAuth 调用这里注册的 WechatAuthService。
        PlatformServices.WechatAuth = new WechatAuthService(this);
        base.OnCreate(savedInstanceState);
    }
}
