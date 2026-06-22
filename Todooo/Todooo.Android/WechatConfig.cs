namespace Todooo.Android;

internal static class WechatConfig
{
    // 用独立占位常量判断 AppId 是否已配置，避免把真实 AppId 写进判断条件后导致“已填写仍被认为未填写”。
    private const string PlaceholderAppId = "YOUR_WECHAT_APP_ID";

    // 微信开放平台 Android 应用的 AppId；包名、签名和这里的 AppId 必须与开放平台后台登记信息一致。
    public const string AppId = "wxa5a23ce9d4a7b8b7";

    // snsapi_userinfo 用于移动应用授权登录，微信回调会返回临时 code。
    public const string Scope = "snsapi_userinfo";

    // 必须是 "{ApplicationId}.wxapi.WXEntryActivity"，否则微信客户端无法把授权结果回调回本应用。
    public const string WxEntryActivityName = "com.mds.dogshop.wxapi.WXEntryActivity";

    // 只校验客户端能否发起微信授权；code 换 token/openid/unionid 应在服务端完成，不能把 AppSecret 放进 App。
    public static bool HasAppId => !string.IsNullOrWhiteSpace(AppId) && AppId != PlaceholderAppId;
}
