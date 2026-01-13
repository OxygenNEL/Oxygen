using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OxygenNEL.Component;
using OxygenNEL.Manager;
using Serilog;

namespace OxygenNEL.Page;

public sealed partial class LoginPage : Microsoft.UI.Xaml.Controls.Page
{
    bool _registerCodeVerified;

    public LoginPage()
    {
        InitializeComponent();
    }

    void ModeNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            var isLogin = tag == "login";
            LoginPanel.Visibility = isLogin ? Visibility.Visible : Visibility.Collapsed;
            LoginPanel.Opacity = isLogin ? 1 : 0;
            RegisterPanel.Visibility = isLogin ? Visibility.Collapsed : Visibility.Visible;
            RegisterPanel.Opacity = isLogin ? 0 : 1;
        }
    }

    async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        SetLoading(LoginButtonText, LoginProgress, LoginButton, true);
        try
        {
            var user = (LoginUserInput.Text ?? string.Empty).Trim();
            var pass = LoginPasswordInput.Password ?? string.Empty;
            if (string.IsNullOrWhiteSpace(user)) { NotificationHost.ShowGlobal("请输入用户名或邮箱", ToastLevel.Warning); return; }
            if (string.IsNullOrWhiteSpace(pass)) { NotificationHost.ShowGlobal("请输入密码", ToastLevel.Warning); return; }

            var res = await AuthManager.Instance.LoginAsync(user, pass);
            if (!res.Success)
            {
                Log.Warning("登录失败 | 用户: {User} | 返回: {@Result}", user, res);
                NotificationHost.ShowGlobal(res.Message, ToastLevel.Error);
                return;
            }
            NotificationHost.ShowGlobal("登录成功", ToastLevel.Success);
            MainWindow.RefreshAuthUi();
        }
        finally
        {
            SetLoading(LoginButtonText, LoginProgress, LoginButton, false);
        }
    }

    async void SendCodeButton_Click(object sender, RoutedEventArgs e)
    {
        SetLoading(SendCodeButtonText, SendCodeProgress, SendCodeButton, true);
        try
        {
            var qq = (RegEmailInput.Text ?? string.Empty).Trim();
            if (!IsQQNumber(qq)) { NotificationHost.ShowGlobal("QQ号格式不正确", ToastLevel.Warning); return; }
            var email = qq + "@qq.com";
            var res = await AuthManager.Instance.SendRegisterMailAsync(email);
            if (!res.Success)
            {
                Log.Warning("发送验证码失败 | 邮箱: {Email} | 返回: {@Result}", email, res);
                NotificationHost.ShowGlobal(res.Message, ToastLevel.Error);
                return;
            }
            _registerCodeVerified = false;
            NotificationHost.ShowGlobal(res.Message, ToastLevel.Success);
        }
        finally
        {
            SetLoading(SendCodeButtonText, SendCodeProgress, SendCodeButton, false);
        }
    }

    async void NextStepButton_Click(object sender, RoutedEventArgs e)
    {
        SetLoading(NextStepButtonText, NextStepProgress, NextStepButton, true);
        try
        {
            var qq = (RegEmailInput.Text ?? string.Empty).Trim();
            var code = (RegCodeInput.Text ?? string.Empty).Trim();
            if (!IsQQNumber(qq)) { NotificationHost.ShowGlobal("QQ号格式不正确", ToastLevel.Warning); return; }
            if (!Regex.IsMatch(code, "^[0-9]{6}$")) { NotificationHost.ShowGlobal("验证码必须是6位数字", ToastLevel.Warning); return; }
            var email = qq + "@qq.com";
            var res = await AuthManager.Instance.VerifyCodeAsync(email, code);
            if (!res.Success)
            {
                Log.Warning("验证码验证失败 | 邮箱: {Email} | 返回: {@Result}", email, res);
                NotificationHost.ShowGlobal(res.Message, ToastLevel.Error);
                return;
            }
            NotificationHost.ShowGlobal(res.Message, ToastLevel.Success);
            _registerCodeVerified = true;
            RegisterStep1.Visibility = Visibility.Collapsed;
            RegisterStep2.Visibility = Visibility.Visible;
        }
        finally
        {
            SetLoading(NextStepButtonText, NextStepProgress, NextStepButton, false);
        }
    }

    async void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        SetLoading(RegisterButtonText, RegisterProgress, RegisterButton, true);
        try
        {
            var qq = (RegEmailInput.Text ?? string.Empty).Trim();
            if (!IsQQNumber(qq)) { NotificationHost.ShowGlobal("QQ号格式不正确", ToastLevel.Warning); return; }
            if (!_registerCodeVerified) { NotificationHost.ShowGlobal("请先完成邮箱验证码验证", ToastLevel.Warning); return; }

            var email = qq + "@qq.com";
            var username = (RegUsernameInput.Text ?? string.Empty).Trim();
            var password = RegPasswordInput.Password ?? string.Empty;
            var password2 = RegPassword2Input.Password ?? string.Empty;

            if (username.Length < 3 || username.Length > 64) { NotificationHost.ShowGlobal("用户名长度必须在 3-64 字符", ToastLevel.Warning); return; }
            if (password.Length < 8) { NotificationHost.ShowGlobal("密码至少8位", ToastLevel.Warning); return; }
            if (!string.Equals(password, password2, StringComparison.Ordinal)) { NotificationHost.ShowGlobal("两次输入的密码不一致", ToastLevel.Warning); return; }

            var res = await AuthManager.Instance.RegisterNextAsync(email, username, password);
            if (!res.Success)
            {
                Log.Warning("注册失败 | 邮箱: {Email} | 用户名: {Username} | 返回: {@Result}", email, username, res);
                NotificationHost.ShowGlobal(res.Message, ToastLevel.Error);
                return;
            }
            NotificationHost.ShowGlobal("注册成功", ToastLevel.Success);
            if (AuthManager.Instance.IsLoggedIn) MainWindow.RefreshAuthUi();

            RegisterStep2.Visibility = Visibility.Collapsed;
        }
        finally
        {
            SetLoading(RegisterButtonText, RegisterProgress, RegisterButton, false);
        }
    }

    static void SetLoading(TextBlock textBlock, ProgressRing ring, Control control, bool loading)
    {
        textBlock.Visibility = loading ? Visibility.Collapsed : Visibility.Visible;
        ring.IsActive = loading;
        ring.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
        control.IsEnabled = !loading;
    }

    static bool IsEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return Regex.IsMatch(email, "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$");
    }

    static bool IsQQNumber(string qq)
    {
        if (string.IsNullOrWhiteSpace(qq)) return false;
        return Regex.IsMatch(qq, "^[1-9][0-9]{4,10}$");
    }
}
