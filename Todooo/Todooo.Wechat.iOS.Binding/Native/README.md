# WeChat iOS OpenSDK

Put the official `WechatOpenSDK.xcframework` in this folder:

```text
Todooo.Wechat.iOS.Binding/Native/WechatOpenSDK.xcframework
```

The Avalonia iOS app references the SDK through the .NET iOS binding project. CocoaPods can be used to download the SDK in a temporary native project, but the final Avalonia build consumes the extracted `.xcframework` through `NativeReference`.
