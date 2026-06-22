# Avalonia 12 Android 接入微信 SDK 与微信登录步骤

本文记录本项目在 Avalonia 12 Android 平台接入 `com.tencent.mm.opensdk:wechat-sdk-android` 并实现微信登录的完整过程。当前 Android 包名为 `com.mds.dogshop`，微信回调 Activity 为 `com.mds.dogshop.wxapi.WXEntryActivity`。

## 1. 前置条件

1. 微信开放平台已创建移动应用。
2. 开放平台后台登记的 Android 包名必须是项目实际包名：`com.mds.dogshop`。
3. 开放平台后台登记的应用签名必须与 Release APK 使用的 keystore 一致。
4. Android 真机已安装微信客户端。
5. 客户端只保存微信 `AppId`，不要保存 `AppSecret`。`code` 换 `access_token/openid/unionid` 必须由服务端完成。

## 2. Android 项目配置

Android 工程文件位于 `Todooo/Todooo.Android/Todooo.Android.csproj`。

关键配置：

```xml
<ApplicationId>com.mds.dogshop</ApplicationId>
<AndroidPackageFormat>apk</AndroidPackageFormat>
<AndroidMavenLibrary Include="com.tencent.mm.opensdk:wechat-sdk-android" Version="6.8.34" Bind="true" />
<TransformFile Include="Transforms\Metadata.xml" />
```

说明：

- `.NET Android` 的 `AndroidMavenLibrary` 会从 Maven 下载并绑定 AAR。
- Gradle 可以写 `com.tencent.mm.opensdk:wechat-sdk-android:+`，但 `AndroidMavenLibrary` 不支持 `+` 动态版本，必须写实际版本号。
- 当前 Maven Central 最新可用版本为 `6.8.34`。
- 微信 AAR 内部有少量 Java 成员生成 C# 绑定时会重名，因此使用 `Transforms/Metadata.xml` 重命名冲突成员。

绑定冲突修复文件：

```xml
<metadata>
  <attr path="/api/package[@name='com.tencent.mm.opensdk.modelbase']/class[@name='BaseResp']/field[@name='errCode']" name="managedName">ErrorCode</attr>
  <attr path="/api/package[@name='com.tencent.mm.opensdk.modelmsg']/class[@name='WXMediaMessage']/field[@name='mediaObject']" name="managedName">MessageMediaObject</attr>
</metadata>
```

## 3. AndroidManifest 配置

Manifest 位于 `Todooo/Todooo.Android/Properties/AndroidManifest.xml`。

需要保留网络权限，并声明 Android 11+ 包可见性查询：

```xml
<uses-permission android:name="android.permission.INTERNET" />
<queries>
  <package android:name="com.tencent.mm" />
</queries>
```

`queries` 用于让应用能检测微信是否安装。否则 Android 11 及以上系统可能导致 `IsWXAppInstalled` 返回异常结果。

## 4. 微信配置文件

微信配置集中在 `Todooo/Todooo.Android/WechatConfig.cs`：

```csharp
internal static class WechatConfig
{
    private const string PlaceholderAppId = "YOUR_WECHAT_APP_ID";

    public const string AppId = "微信开放平台 AppId";
    public const string Scope = "snsapi_userinfo";
    public const string WxEntryActivityName = "com.mds.dogshop.wxapi.WXEntryActivity";

    public static bool HasAppId => !string.IsNullOrWhiteSpace(AppId) && AppId != PlaceholderAppId;
}
```

注意：

- `WxEntryActivityName` 必须等于 `{ApplicationId}.wxapi.WXEntryActivity`。
- `HasAppId` 必须与固定占位值比较，不要把真实 AppId 写进判断条件。
- 微信开放平台后台的包名和签名必须与当前 APK 一致，否则无法正常拉起或回调。

## 5. 注册 Android 平台服务

共享层不直接引用 Android SDK，而是通过 `Todooo.Services.PlatformServices` 暴露平台能力。

Android 入口 `Todooo/Todooo.Android/MainActivity.cs` 在启动时注册实现：

```csharp
protected override void OnCreate(Bundle? savedInstanceState)
{
    PlatformServices.WechatAuth = new WechatAuthService(this);
    base.OnCreate(savedInstanceState);
}
```

这样共享层 `MainViewModel` 可以调用 `PlatformServices.WechatAuth.LoginAsync()`，但不会依赖 Android 命名空间。

## 6. 发起微信登录

核心实现位于 `Todooo/Todooo.Android/WechatAuthService.cs`。

流程：

1. 创建微信 API：

   ```csharp
   _api = WXAPIFactory.CreateWXAPI(activity, WechatConfig.AppId, true)!;
   _api.RegisterApp(WechatConfig.AppId);
   ```

2. 登录前检查 AppId 和微信客户端：

   ```csharp
   if (!WechatConfig.HasAppId) ...
   if (!_api.IsWXAppInstalled) ...
   ```

3. 生成 `state` 并保存等待中的 `TaskCompletionSource`：

   ```csharp
   string state = Guid.NewGuid().ToString("N");
   _pendingLogin = new TaskCompletionSource<WechatAuthResult>(TaskCreationOptions.RunContinuationsAsynchronously);
   _pendingState = state;
   ```

4. 发送微信授权请求：

   ```csharp
   var request = new SendAuth.Req
   {
       Scope = WechatConfig.Scope,
       State = state
   };

   _api.SendReq(request);
   ```

5. 等待 `WXEntryActivity` 回调，并将回调结果转换为 `WechatAuthResult`。

客户端成功后只会得到临时 `code`。后续业务登录应把 `code` 发送给后端，由后端使用 AppSecret 调用微信接口换取 `access_token/openid/unionid`。

## 7. 接收微信回调

微信要求 Android 应用提供固定路径的回调 Activity：

```text
{ApplicationId}.wxapi.WXEntryActivity
```

本项目通过 `ActivityAttribute.Name` 生成这个 Java 类名：

```csharp
[Activity(
    Name = WechatConfig.WxEntryActivityName,
    Exported = true,
    LaunchMode = LaunchMode.SingleTop,
    NoHistory = true)]
public sealed class WXEntryActivity : Activity, IWXAPIEventHandler
```

回调处理：

1. `OnCreate` / `OnNewIntent` 调用 `WechatAuthService.HandleWechatIntent(intent, this)`。
2. 微信 SDK 解析 Intent 后触发 `OnResp(BaseResp? response)`。
3. `OnResp` 调用 `WechatAuthService.DispatchWechatResponse(response)`。
4. `WechatAuthService` 校验回调类型、`state`、错误码和 `code`。
5. 成功时返回 `WechatAuthResult.Success(code, state)`。

## 8. 共享层 ViewModel 调用

`Todooo/Todooo/ViewModels/MainViewModel.cs` 通过 MVVM Toolkit 命令触发登录：

```csharp
[RelayCommand(CanExecute = nameof(CanWechatLogin))]
private async Task WechatLoginAsync()
{
    var result = await PlatformServices.WechatAuth.LoginAsync();
}
```

UI 绑定位于 `Todooo/Todooo/Views/MainView.axaml`：

```xml
<Button Command="{Binding WechatLoginCommand}">
    <Button.Content>
        <TextBlock Text="微信" FontSize="12" />
    </Button.Content>
</Button>
```

当前界面会显示授权状态和返回的临时 `code`，方便真机调试。接入后端后，应改为发送 `code` 到服务端并展示真实登录状态。

## 9. Release 打包与签名

Release 配置在 Android csproj 中：

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <AndroidPackageFormat>apk</AndroidPackageFormat>
  <AndroidCreatePackagePerAbi>false</AndroidCreatePackagePerAbi>
  <DebugSymbols>false</DebugSymbols>
  <DebugType>none</DebugType>
  <DOGSHOP_ANDROID_KEYSTORE Condition="'$(DOGSHOP_ANDROID_KEYSTORE)' == ''">..\..\mds_dog_unity.keystore</DOGSHOP_ANDROID_KEYSTORE>
</PropertyGroup>
```

keystore 放在仓库根目录：

```text
mds_dog_unity.keystore
```

为了防止密钥文件误提交，`.gitignore` 已加入：

```gitignore
*.keystore
```

打包命令：

```powershell
dotnet publish Todooo.Android\Todooo.Android.csproj -c Release
```

产物路径：

```text
Todooo/Todooo.Android/bin/Release/net10.0-android/com.mds.dogshop-Signed.apk
```

## 10. 常见问题

### 1. 点击按钮提示未填写 AppId

检查 `WechatConfig.HasAppId` 是否仍在与真实 AppId 比较。正确做法是与占位值比较：

```csharp
private const string PlaceholderAppId = "YOUR_WECHAT_APP_ID";
public static bool HasAppId => !string.IsNullOrWhiteSpace(AppId) && AppId != PlaceholderAppId;
```

### 2. 提示未安装微信

确认真机安装了微信，并检查 Manifest 是否包含：

```xml
<queries>
  <package android:name="com.tencent.mm" />
</queries>
```

### 3. 微信无法回调

检查三项是否完全一致：

- `ApplicationId`：`com.mds.dogshop`
- `WxEntryActivityName`：`com.mds.dogshop.wxapi.WXEntryActivity`
- 微信开放平台后台登记的包名和签名

### 4. Release 包无法登录，但 Debug 可以

通常是签名不一致。微信开放平台登记的是签名摘要，Debug keystore 和 Release keystore 签名不同。必须使用开放平台登记过的 Release keystore 重新打包。

### 5. AndroidMavenLibrary 写 `Version="+"` 下载失败

`.NET Android` 不支持 Gradle 动态版本解析，会把 `+` 当成字面版本下载，导致 Maven 404。应固定为实际版本，例如：

```xml
<AndroidMavenLibrary Include="com.tencent.mm.opensdk:wechat-sdk-android" Version="6.8.34" Bind="true" />
```

## 11. 验证命令

Debug 构建：

```powershell
dotnet build Todooo.Android\Todooo.Android.csproj
```

Release 发布：

```powershell
dotnet publish Todooo.Android\Todooo.Android.csproj -c Release
```

检查生成 Manifest：

```powershell
Select-String -Path Todooo\Todooo.Android\obj\Release\net10.0-android\AndroidManifest.xml -Pattern "package=|WXEntryActivity"
```

期望结果包含：

```text
package="com.mds.dogshop"
android:name="com.mds.dogshop.wxapi.WXEntryActivity"
```
