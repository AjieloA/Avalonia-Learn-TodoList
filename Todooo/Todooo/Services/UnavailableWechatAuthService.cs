using System.Threading;
using System.Threading.Tasks;

namespace Todooo.Services;

public sealed class UnavailableWechatAuthService : IWechatAuthService
{
    public bool IsAvailable => false;

    public Task<WechatAuthResult> LoginAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(WechatAuthResult.Failed(-1, "当前平台未接入微信登录"));
    }
}
