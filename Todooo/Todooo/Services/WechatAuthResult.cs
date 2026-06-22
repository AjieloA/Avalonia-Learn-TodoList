namespace Todooo.Services;

public sealed record WechatAuthResult(
    bool IsSuccess,
    // 微信授权成功后返回的临时 code；有效期很短，只能由服务端使用 AppSecret 换取用户身份信息。
    string? Code,
    // 发起授权时生成的 state，回调时用于确认结果属于本次请求。
    string? State,
    int ErrorCode,
    string? ErrorMessage)
{
    public static WechatAuthResult Success(string code, string? state)
    {
        return new WechatAuthResult(true, code, state, 0, null);
    }

    public static WechatAuthResult Failed(int errorCode, string errorMessage, string? state = null)
    {
        return new WechatAuthResult(false, null, state, errorCode, errorMessage);
    }
}
