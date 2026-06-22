namespace Todooo.Services;

public static class PlatformServices
{
    public static IWechatAuthService WechatAuth { get; set; } = new UnavailableWechatAuthService();
}
