using System;

namespace Todooo.iOS;

internal static class WechatConfig
{
    private const string PlaceholderAppId = "YOUR_WECHAT_APP_ID";
    private const string PlaceholderUniversalLink = "https://YOUR_DOMAIN/YOUR_PATH/";

    // iOS AppId should match the mobile app registered in the WeChat Open Platform.
    public const string AppId = "wxa5a23ce9d4a7b8b7";

    // Replace this with the Universal Link configured in the WeChat Open Platform.
    // The domain must also be present in Entitlements.plist as applinks:<domain>.
    public const string UniversalLink = PlaceholderUniversalLink;

    public const string Scope = "snsapi_userinfo";

    public static bool HasAppId => !string.IsNullOrWhiteSpace(AppId) && AppId != PlaceholderAppId;

    public static bool HasUniversalLink =>
        !string.IsNullOrWhiteSpace(UniversalLink) &&
        UniversalLink != PlaceholderUniversalLink &&
        Uri.TryCreate(UniversalLink, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps;

    public static bool IsConfigured => HasAppId && HasUniversalLink;
}
