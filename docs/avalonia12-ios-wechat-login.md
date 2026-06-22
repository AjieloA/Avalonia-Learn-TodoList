# Avalonia 12 iOS 接入微信 SDK 与微信登录步骤

本文记录本项目在 Avalonia 12 iOS 平台接入微信 iOS OpenSDK 并实现微信登录的项目结构和配置点。

## 1. 已完成的项目改动

- 新增 `Todooo.Wechat.iOS.Binding`，用于把微信官方 `WechatOpenSDK.xcframework` 绑定到 .NET iOS。
- `Todooo.iOS` 引用 binding 项目，并在 `AppDelegate` 中注册微信 SDK。
- `Todooo.iOS.Services.WechatAuthService` 实现共享层的 `IWechatAuthService`。
- `Info.plist` 已加入微信 URL Scheme 和 `LSApplicationQueriesSchemes`。
- `Entitlements.plist` 已加入 Associated Domains 配置项。
- 共享层 `MainViewModel` 继续调用 `PlatformServices.WechatAuth.LoginAsync()`，不直接依赖 iOS SDK。

## 2. 放置微信 iOS SDK

Avalonia iOS 项目不能直接像 Xcode 原生工程那样用 CocoaPods 接管构建。可以用 CocoaPods 下载 SDK，但最终要把官方 framework 放到 binding 项目中：

```text
Todooo/Todooo.Wechat.iOS.Binding/Native/WechatOpenSDK.xcframework
```

缺少该文件时，正常 iOS 构建会报错：

```text
Missing WechatOpenSDK.xcframework. Put the official WeChat iOS OpenSDK at Todooo.Wechat.iOS.Binding/Native/WechatOpenSDK.xcframework before building the iOS app.
```

如果只想检查 C# 编译，不做最终链接，可以临时使用：

```powershell
dotnet build Todooo.iOS\Todooo.iOS.csproj --no-restore /p:AllowMissingWechatOpenSdkForCompile=true
```

## 3. 微信开放平台配置

微信开放平台 iOS 应用至少需要配置：

1. `Bundle ID`：当前项目为 `com.mds.dogshop`，位于 `Todooo.iOS/Info.plist`。
2. `AppId`：当前代码使用 `wxa5a23ce9d4a7b8b7`，位于 `Todooo.iOS/WechatConfig.cs`。
3. `Universal Link`：必须替换 `Todooo.iOS/WechatConfig.cs` 中的 `https://YOUR_DOMAIN/YOUR_PATH/`。
4. Associated Domains：必须替换 `Todooo.iOS/Entitlements.plist` 中的 `applinks:YOUR_DOMAIN`。

Universal Link 的域名服务器还需要提供 Apple 要求的 `apple-app-site-association` 文件：

```text
https://YOUR_DOMAIN/.well-known/apple-app-site-association
```

## 4. 登录流程

1. `AppDelegate.FinishedLaunching` 创建 `WechatAuthService` 并调用 `WXApi.RegisterApp(appId, universalLink)`。
2. 用户点击微信登录按钮后，共享层调用 `PlatformServices.WechatAuth.LoginAsync()`。
3. iOS 端创建 `SendAuthReq`，写入 `scope = snsapi_userinfo` 和随机 `state`，然后调用 `WXApi.SendReq`。
4. 微信客户端授权后回跳 App。
5. `AppDelegate.OpenUrl` 或 `AppDelegate.ContinueUserActivity` 把回调交给 `WXApi`。
6. 微信 SDK 触发 `onResp:`，`WechatAuthService` 校验 `state` 并返回临时 `code`。
7. 客户端只把 `code` 发给业务服务端，不能在 App 内保存或使用 `AppSecret`。

## 5. 验证命令

检查 C# 层：

```powershell
dotnet build Todooo.iOS\Todooo.iOS.csproj --no-restore /p:AllowMissingWechatOpenSdkForCompile=true
```

放入官方 `WechatOpenSDK.xcframework` 并配置 Universal Link 后，执行正常构建：

```powershell
dotnet build Todooo.iOS\Todooo.iOS.csproj --no-restore
```

真机验证时必须确认：

- 设备安装了微信。
- Bundle ID 与微信开放平台登记一致。
- AppId 与微信开放平台登记一致。
- Universal Link 已通过 Apple 和微信开放平台校验。
- Associated Domains 已包含真实域名。
