namespace Todooo.Android;

internal static class WechatConfig
{
    private const string PlaceholderAppId = "YOUR_WECHAT_APP_ID";

    public const string AppId = "wxa5a23ce9d4a7b8b7";
    public const string Scope = "snsapi_userinfo";

    // Must be "{ApplicationId}.wxapi.WXEntryActivity" and match the package registered in WeChat Open Platform.
    public const string WxEntryActivityName = "com.mds.dogshop.wxapi.WXEntryActivity";

    public static bool HasAppId => !string.IsNullOrWhiteSpace(AppId) && AppId != PlaceholderAppId;
}
