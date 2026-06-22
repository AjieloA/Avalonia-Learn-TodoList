using System;
using Foundation;
using ObjCRuntime;

namespace Todooo.Wechat.iOS.Binding;

[BaseType(typeof(NSObject))]
interface WXApi
{
    [Static]
    [Export("registerApp:universalLink:")]
    bool RegisterApp(string appId, string universalLink);

    [Static]
    [Export("isWXAppInstalled")]
    bool IsWXAppInstalled();

    [Static]
    [Export("sendReq:")]
    bool SendReq(BaseReq request);

    [Static]
    [Export("sendReq:completion:")]
    bool SendReq(BaseReq request, Action<bool> completion);

    [Static]
    [Export("handleOpenURL:delegate:")]
    bool HandleOpenUrl(NSUrl url, NSObject wxDelegate);

    [Static]
    [Export("handleOpenUniversalLink:delegate:")]
    bool HandleOpenUniversalLink(NSUserActivity userActivity, NSObject wxDelegate);
}

[BaseType(typeof(NSObject))]
interface BaseReq
{
    [Export("type")]
    int Type { get; }
}

[BaseType(typeof(NSObject))]
interface BaseResp
{
    [Export("errCode")]
    int ErrorCode { get; set; }

    [NullAllowed]
    [Export("errStr")]
    string ErrorMessage { get; set; }

    [Export("type")]
    int Type { get; }
}

[BaseType(typeof(BaseReq), Name = "SendAuthReq")]
interface SendAuthReq
{
    [Export("scope")]
    string Scope { get; set; }

    [NullAllowed]
    [Export("state")]
    string State { get; set; }
}

[BaseType(typeof(BaseResp), Name = "SendAuthResp")]
interface SendAuthResp
{
    [NullAllowed]
    [Export("code")]
    string Code { get; set; }

    [NullAllowed]
    [Export("state")]
    string State { get; set; }

    [NullAllowed]
    [Export("lang")]
    string Lang { get; set; }

    [NullAllowed]
    [Export("country")]
    string Country { get; set; }
}
