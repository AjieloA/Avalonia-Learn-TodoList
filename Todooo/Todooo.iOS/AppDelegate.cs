using Foundation;
using UIKit;
using Avalonia;
using Avalonia.Controls;
using Avalonia.iOS;
using Avalonia.Media;
using Todooo.iOS.Services;
using Todooo.Services;

namespace Todooo.iOS;

// The UIApplicationDelegate for the application. This class is responsible for launching the 
// User Interface of the application, as well as listening (and optionally responding) to 
// application events from iOS.
[Register("AppDelegate")]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public partial class AppDelegate : AvaloniaAppDelegate<App>
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    [Export("application:didFinishLaunchingWithOptions:")]
    public new bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        var wechatAuthService = new WechatAuthService();
        PlatformServices.WechatAuth = wechatAuthService;
        wechatAuthService.RegisterApp();

        return base.FinishedLaunching(application, launchOptions);
    }

    [Export("application:openURL:options:")]
    public new bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
    {
        return WechatAuthService.Current?.HandleOpenUrl(url) == true ||
               base.OpenUrl(app, url, options);
    }

    [Export("application:continueUserActivity:restorationHandler:")]
    public new bool ContinueUserActivity(
        UIApplication application,
        NSUserActivity userActivity,
        UIApplicationRestorationHandler completionHandler)
    {
        return WechatAuthService.Current?.HandleUniversalLink(userActivity) == true ||
               base.ContinueUserActivity(application, userActivity, completionHandler);
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
