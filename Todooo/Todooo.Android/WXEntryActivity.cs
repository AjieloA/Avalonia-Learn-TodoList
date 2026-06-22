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
        HandleWechatCallback(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        HandleWechatCallback(intent);
    }

    public void OnReq(BaseReq? request)
    {
    }

    public void OnResp(BaseResp? response)
    {
        WechatAuthService.DispatchWechatResponse(response);
        Finish();
    }

    private void HandleWechatCallback(Intent? intent)
    {
        if (!WechatAuthService.HandleWechatIntent(intent, this))
        {
            Finish();
        }
    }
}
