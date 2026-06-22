namespace Todooo.Services;

public static class PlatformServices
{
    // 共享项目通过这个入口访问平台能力；非 Android 平台默认返回不可用实现，避免直接引用 Android SDK。
    public static IWechatAuthService WechatAuth { get; set; } = new UnavailableWechatAuthService();
}
