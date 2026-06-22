using System;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using ObjCRuntime;
using Todooo.Services;
using Todooo.Wechat.iOS.Binding;

namespace Todooo.iOS.Services;

internal sealed class WechatAuthService : NSObject, IWechatAuthService
{
    private readonly object _syncRoot = new();
    private TaskCompletionSource<WechatAuthResult>? _pendingLogin;
    private string? _pendingState;

    public WechatAuthService()
    {
        Current = this;
    }

    public static WechatAuthService? Current { get; private set; }

    public bool IsAvailable => WechatConfig.IsConfigured && WXApi.IsWXAppInstalled();

    public bool RegisterApp()
    {
        if (!WechatConfig.IsConfigured)
        {
            return false;
        }

        return WXApi.RegisterApp(WechatConfig.AppId, WechatConfig.UniversalLink);
    }

    public async Task<WechatAuthResult> LoginAsync(CancellationToken cancellationToken = default)
    {
        if (!WechatConfig.HasAppId)
        {
            return WechatAuthResult.Failed(-1, "请先在 iOS WechatConfig.AppId 中填写微信开放平台 AppId");
        }

        if (!WechatConfig.HasUniversalLink)
        {
            return WechatAuthResult.Failed(-8, "请先在 iOS WechatConfig.UniversalLink 中填写微信开放平台配置的 Universal Link");
        }

        if (!WXApi.IsWXAppInstalled())
        {
            return WechatAuthResult.Failed(-2, "当前设备未安装微信");
        }

        TaskCompletionSource<WechatAuthResult> pendingLogin;
        string state = Guid.NewGuid().ToString("N");

        lock (_syncRoot)
        {
            if (_pendingLogin is { Task.IsCompleted: false })
            {
                return WechatAuthResult.Failed(-3, "已有微信授权请求正在进行");
            }

            pendingLogin = new TaskCompletionSource<WechatAuthResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingLogin = pendingLogin;
            _pendingState = state;
        }

        using var cancellationRegistration = cancellationToken.Register(() =>
            CompletePendingLogin(WechatAuthResult.Failed(-4, "微信授权已取消", state)));

        var request = new SendAuthReq
        {
            Scope = WechatConfig.Scope,
            State = state
        };

        bool sent = WXApi.SendReq(request, success =>
        {
            if (!success)
            {
                CompletePendingLogin(WechatAuthResult.Failed(-5, "微信授权请求发送失败", state));
            }
        });

        if (!sent)
        {
            CompletePendingLogin(WechatAuthResult.Failed(-5, "微信授权请求发送失败", state));
        }

        return await pendingLogin.Task.ConfigureAwait(false);
    }

    public bool HandleOpenUrl(NSUrl? url)
    {
        return url is not null && WXApi.HandleOpenUrl(url, this);
    }

    public bool HandleUniversalLink(NSUserActivity? userActivity)
    {
        return userActivity is not null && WXApi.HandleOpenUniversalLink(userActivity, this);
    }

    [Export("onReq:")]
    public void OnReq(BaseReq request)
    {
        // 当前只接入微信登录，不处理微信向 App 主动发起的请求。
    }

    [Export("onResp:")]
    public void OnResp(BaseResp response)
    {
        OnWechatResponse(response);
    }

    private void OnWechatResponse(BaseResp? response)
    {
        if (response is not SendAuthResp authResponse)
        {
            CompletePendingLogin(WechatAuthResult.Failed(-6, "收到非微信登录回调"));
            return;
        }

        string? responseState = authResponse.State;
        string? pendingState;

        lock (_syncRoot)
        {
            pendingState = _pendingState;
        }

        if (!string.IsNullOrWhiteSpace(pendingState) && responseState != pendingState)
        {
            CompletePendingLogin(WechatAuthResult.Failed(-7, "微信授权 state 校验失败", responseState));
            return;
        }

        if (authResponse.ErrorCode == 0 && !string.IsNullOrWhiteSpace(authResponse.Code))
        {
            CompletePendingLogin(WechatAuthResult.Success(authResponse.Code, responseState));
            return;
        }

        CompletePendingLogin(WechatAuthResult.Failed(authResponse.ErrorCode, GetWechatErrorMessage(authResponse), responseState));
    }

    private void CompletePendingLogin(WechatAuthResult result)
    {
        TaskCompletionSource<WechatAuthResult>? pendingLogin;

        lock (_syncRoot)
        {
            pendingLogin = _pendingLogin;
            _pendingLogin = null;
            _pendingState = null;
        }

        pendingLogin?.TrySetResult(result);
    }

    private static string GetWechatErrorMessage(BaseResp response)
    {
        if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
        {
            return response.ErrorMessage;
        }

        return response.ErrorCode switch
        {
            -2 => "用户取消微信授权",
            -4 => "微信授权被拒绝",
            -5 => "微信授权暂不支持",
            0 => "微信授权成功",
            _ => $"微信授权失败，错误码 {response.ErrorCode}"
        };
    }
}
