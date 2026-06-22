using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Todooo.Services;

namespace Todooo.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private string _mainTitleTxt = "Todoooo";
    public ObservableCollection<TodoListItemViewModel>? TodoListItem { get; } = new();
    [ObservableProperty] private TodoListItemViewModel? _selectTodoListItem;
    // 页面只展示客户端授权状态和临时 code；真正登录态应由业务服务端确认后再写入本地账号状态。
    [ObservableProperty] private string _wechatLoginStatus = PlatformServices.WechatAuth.IsAvailable ? "微信登录未授权" : "当前平台不可用";
    [ObservableProperty] private string? _wechatAuthCode;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(WechatLoginCommand))]
    private bool _isWechatLoginBusy;

    private Dictionary<int, string> imgDic = new Dictionary<int, string>()
    {
        { 1, "https://picture-new.88dog.com/g5ty9mai1699269161.png" },
        { 2, "https://picture-new.88dog.com/i79fydky1699269214.png" },
        { 3, "https://picture-new.88dog.com/yabypiac1743563877.png" },
        { 4, "https://picture-new.88dog.com/x615b0rt1699269295.png" },
        { 5, "https://picture-new.88dog.com/kw36kgsy1699268926.png" },
        { 6, "https://picture-new.88dog.com/anc2a12c1699269136.png" },
        { 7, "https://picture-new.88dog.com/drqm3lgd1779689146.jpg" }
    };

    public MainViewModel()
    {
        var _random = new Random();
        for (int i = 0; i < 100000; i++)
        {
            var _idx = _random.Next(1, 8);
            if (!imgDic.TryGetValue(_idx, out var _url))
                _url = "https://picture-new.88dog.com/avatar.png";
            TodoListItem?.Add(new TodoListItemViewModel()
            {
                Name = $"Name-{i}",
                Description = $"Description - - {i}",
                ImageUrl = _url
            });
        }
    }

    private bool CanWechatLogin()
    {
        return !IsWechatLoginBusy;
    }

    [RelayCommand(CanExecute = nameof(CanWechatLogin))]
    private async Task WechatLoginAsync()
    {
        // RelayCommand 会根据 IsWechatLoginBusy 自动刷新按钮可用状态，避免重复唤起微信。
        IsWechatLoginBusy = true;
        WechatAuthCode = null;
        WechatLoginStatus = "正在唤起微信...";

        try
        {
            var result = await PlatformServices.WechatAuth.LoginAsync();
            if (result.IsSuccess)
            {
                WechatAuthCode = result.Code;
                // 这里先直接显示 code，便于真机联调；接入服务端后应把 code 发送给后端换取用户信息。
                WechatLoginStatus = $"微信授权成功，code: {result.Code}";
                return;
            }

            WechatLoginStatus = $"微信授权失败({result.ErrorCode}): {result.ErrorMessage}";
        }
        catch (Exception ex)
        {
            WechatLoginStatus = $"微信授权异常: {ex.Message}";
        }
        finally
        {
            IsWechatLoginBusy = false;
        }
    }
}
