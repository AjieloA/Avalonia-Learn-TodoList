using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Com.Tencent.MM.Opensdk.Modelbase;
using Com.Tencent.MM.Opensdk.Modelmsg;
using Com.Tencent.MM.Opensdk.Openapi;
using Todooo.Services;

namespace Todooo.Android;

internal sealed class WechatAuthService : IWechatAuthService
{
    private readonly object _syncRoot = new();
    private readonly IWXAPI _api;
    private TaskCompletionSource<WechatAuthResult>? _pendingLogin;
    private string? _pendingState;

    public WechatAuthService(Activity activity)
    {
        _api = WXAPIFactory.CreateWXAPI(activity, WechatConfig.AppId, true)!;

        if (WechatConfig.HasAppId)
        {
            _api.RegisterApp(WechatConfig.AppId);
        }

        Current = this;
    }

    public static WechatAuthService? Current { get; private set; }

    public bool IsAvailable => WechatConfig.HasAppId && _api.IsWXAppInstalled;

    public async Task<WechatAuthResult> LoginAsync(CancellationToken cancellationToken = default)
    {
        if (!WechatConfig.HasAppId)
        {
            return WechatAuthResult.Failed(-1, "请先在 WechatConfig.AppId 中填写微信开放平台 AppId");
        }

        if (!_api.IsWXAppInstalled)
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

        var request = new SendAuth.Req
        {
            Scope = WechatConfig.Scope,
            State = state
        };

        bool sent = _api.SendReq(request);
        if (!sent)
        {
            CompletePendingLogin(WechatAuthResult.Failed(-5, "微信授权请求发送失败", state));
        }

        return await pendingLogin.Task.ConfigureAwait(false);
    }

    public static bool HandleWechatIntent(Intent? intent, IWXAPIEventHandler handler)
    {
        if (intent is null || Current is null)
        {
            return false;
        }

        return Current._api.HandleIntent(intent, handler);
    }

    public static void DispatchWechatResponse(BaseResp? response)
    {
        Current?.OnWechatResponse(response);
    }

    private void OnWechatResponse(BaseResp? response)
    {
        if (response is not SendAuth.Resp authResponse)
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

        CompletePendingLogin(WechatAuthResult.Failed(authResponse.ErrorCode, GetWechatErrorMessage(authResponse.ErrorCode), responseState));
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

    private static string GetWechatErrorMessage(int errorCode)
    {
        return errorCode switch
        {
            -2 => "用户取消微信授权",
            -4 => "微信授权被拒绝",
            -6 => "微信授权暂不支持",
            0 => "微信授权成功",
            _ => $"微信授权失败，错误码 {errorCode}"
        };
    }
}
