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

    // 微信授权是“发起请求 -> 离开 App -> WXEntryActivity 回调”的跨 Activity 异步流程。
    // 用 TaskCompletionSource 把 SDK 回调转换成 ViewModel 可 await 的任务。
    private TaskCompletionSource<WechatAuthResult>? _pendingLogin;
    private string? _pendingState;

    public WechatAuthService(Activity activity)
    {
        // 第三个参数 true 表示创建 API 实例后检查签名，正式登录要求包名和签名与微信开放平台一致。
        _api = WXAPIFactory.CreateWXAPI(activity, WechatConfig.AppId, true)!;

        if (WechatConfig.HasAppId)
        {
            // registerApp 必须在 sendReq 前调用，否则微信客户端可能拒绝处理本应用请求。
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

        // state 会随请求带给微信，并在回调时原样返回；用它抵御串包或非本次授权回调。
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

        // 如果页面或调用方取消等待，也要释放 pending 状态，避免下一次登录被误认为仍在进行。
        using var cancellationRegistration = cancellationToken.Register(() =>
            CompletePendingLogin(WechatAuthResult.Failed(-4, "微信授权已取消", state)));

        // SendAuth.Req 是微信移动应用登录的标准请求；客户端只能拿到临时 code。
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

    // WXEntryActivity 与 MainActivity 生命周期不同，用静态 Current 找到当前登录服务实例。
    public static void DispatchWechatResponse(BaseResp? response)
    {
        Current?.OnWechatResponse(response);
    }

    private void OnWechatResponse(BaseResp? response)
    {
        // 登录授权只接受 SendAuth.Resp；分享、支付等其它微信回调不应完成登录任务。
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

        // 如果 state 不一致，说明回调不是本次发起的授权请求，直接失败并清理等待状态。
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
            // 先取出再清空，保证并发回调或取消只会完成一次任务。
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
