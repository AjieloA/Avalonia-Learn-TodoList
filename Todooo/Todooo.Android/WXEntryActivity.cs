using Android.App;
using Android.Content;
using Android.OS;
using Com.Tencent.MM.Opensdk.Modelbase;
using Com.Tencent.MM.Opensdk.Openapi;

namespace Todooo.Android;

[Activity(
    Name = WechatConfig.WxEntryActivityName,
    Exported = true,
    LaunchMode = global::Android.Content.PM.LaunchMode.SingleTop,
    NoHistory = true,
    Theme = "@style/MyTheme.NoActionBar")]
public sealed class WXEntryActivity : Activity, IWXAPIEventHandler
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // 微信 SDK 要求回调 Activity 固定放在应用包名的 wxapi 目录下。
        // .NET Android 通过 ActivityAttribute.Name 生成该 Java 类名，收到 Intent 后交给 SDK 解析。
        HandleWechatCallback(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        // singleTop 场景下微信可能复用已有 Activity，需要同时处理新的 Intent。
        HandleWechatCallback(intent);
    }

    public void OnReq(BaseReq? request)
    {
        // 当前只接入登录授权，不处理微信向应用主动发起的请求。
    }

    public void OnResp(BaseResp? response)
    {
        // 授权结果会在这里回到 App，再派发给 WechatAuthService 中等待的 TaskCompletionSource。
        WechatAuthService.DispatchWechatResponse(response);
        Finish();
    }

    private void HandleWechatCallback(Intent? intent)
    {
        // HandleIntent 会校验 Intent 内容并触发 OnResp；解析失败时直接关闭这个透明回调页。
        if (!WechatAuthService.HandleWechatIntent(intent, this))
        {
            Finish();
        }
    }
}
