using System.Threading;
using System.Threading.Tasks;

namespace Todooo.Services;

public interface IWechatAuthService
{
    bool IsAvailable { get; }

    Task<WechatAuthResult> LoginAsync(CancellationToken cancellationToken = default);
}
