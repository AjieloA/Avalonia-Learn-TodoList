namespace Todooo.Services;

public sealed record WechatAuthResult(
    bool IsSuccess,
    string? Code,
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
