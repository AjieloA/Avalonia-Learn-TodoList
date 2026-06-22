using System.Threading;
using System.Threading.Tasks;

namespace Todooo.Services;

public interface IWechatAuthService
{
    // 仅表示当前平台具备发起微信登录的基本条件：Android 已注册 AppId 且设备安装了微信。
    bool IsAvailable { get; }

    // 发起微信移动应用授权。成功时返回微信临时 code，后续应交给服务端换取 access_token/openid/unionid。
    Task<WechatAuthResult> LoginAsync(CancellationToken cancellationToken = default);
}
