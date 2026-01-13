using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using OxygenNEL.Manager;
using Windows.System;

namespace OxygenNEL.Component;

public sealed partial class UserProfileCard : UserControl
{
    public UserProfileCard()
    {
        InitializeComponent();
        Loaded += UserProfileCard_Loaded;
    }

    void UserProfileCard_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateUserInfo();
    }

    public void UpdateUserInfo()
    {
        var username = AuthManager.Instance.Username;
        var userId = AuthManager.Instance.UserId;
        UsernameText.Text = string.IsNullOrWhiteSpace(username) ? "用户" : username;
        UserIdText.Text = userId > 0 ? $"ID: {userId}" : "ID: -";
        UpdateAvatar();
        UpdateRank();

        if (AuthManager.Instance.IsLoggedIn)
        {
            _ = FetchUserInfoAsync();
        }
    }

    void UpdateRank()
    {
        var rank = AuthManager.Instance.Rank;
        var text = string.IsNullOrWhiteSpace(rank) ? "no iq" : rank;
        
        RankPanel.Children.Clear();
        var segments = ParseMinecraftColors(text);
        foreach (var (content, color) in segments)
        {
            RankPanel.Children.Add(new TextBlock
            {
                Text = content,
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(color)
            });
        }
        RankPanel.Visibility = Visibility.Visible;
    }

    static List<(string Text, Windows.UI.Color Color)> ParseMinecraftColors(string text)
    {
        var result = new List<(string, Windows.UI.Color)>();
        var currentColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
        var buffer = "";
        
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '§' && i + 1 < text.Length)
            {
                if (buffer.Length > 0)
                {
                    result.Add((buffer, currentColor));
                    buffer = "";
                }
                currentColor = GetMinecraftColor(text[i + 1]);
                i++;
            }
            else
            {
                buffer += text[i];
            }
        }
        if (buffer.Length > 0) result.Add((buffer, currentColor));
        return result;
    }

    static Windows.UI.Color GetMinecraftColor(char code) => char.ToLower(code) switch
    {
        '0' => Windows.UI.Color.FromArgb(255, 0, 0, 0),
        '1' => Windows.UI.Color.FromArgb(255, 0, 0, 170),
        '2' => Windows.UI.Color.FromArgb(255, 0, 170, 0),
        '3' => Windows.UI.Color.FromArgb(255, 0, 170, 170),
        '4' => Windows.UI.Color.FromArgb(255, 170, 0, 0),
        '5' => Windows.UI.Color.FromArgb(255, 170, 0, 170),
        '6' => Windows.UI.Color.FromArgb(255, 255, 170, 0),
        '7' => Windows.UI.Color.FromArgb(255, 170, 170, 170),
        '8' => Windows.UI.Color.FromArgb(255, 85, 85, 85),
        '9' => Windows.UI.Color.FromArgb(255, 85, 85, 255),
        'a' => Windows.UI.Color.FromArgb(255, 85, 255, 85),
        'b' => Windows.UI.Color.FromArgb(255, 85, 255, 255),
        'c' => Windows.UI.Color.FromArgb(255, 255, 85, 85),
        'd' => Windows.UI.Color.FromArgb(255, 255, 85, 255),
        'e' => Windows.UI.Color.FromArgb(255, 255, 255, 85),
        'f' => Windows.UI.Color.FromArgb(255, 255, 255, 255),
        _ => Windows.UI.Color.FromArgb(255, 255, 255, 255)
    };

    void UpdateAvatar()
    {
        var avatar = AuthManager.Instance.Avatar;
        if (!string.IsNullOrWhiteSpace(avatar))
        {
            try
            {
                var base64Data = avatar;
                if (avatar.Contains(","))
                {
                    base64Data = avatar.Split(',')[1];
                }
                
                var bytes = Convert.FromBase64String(base64Data);
                using var ms = new MemoryStream(bytes);
                var bitmap = new BitmapImage();
                bitmap.SetSource(ms.AsRandomAccessStream());
                AvatarImageBrush.ImageSource = bitmap;
                AvatarImageEllipse.Visibility = Visibility.Visible;
                AvatarEllipse.Visibility = Visibility.Collapsed;
                AvatarIcon.Visibility = Visibility.Collapsed;
            }
            catch
            {
                ShowDefaultAvatar();
            }
        }
        else
        {
            ShowDefaultAvatar();
        }
    }

    void ShowDefaultAvatar()
    {
        AvatarImageEllipse.Visibility = Visibility.Collapsed;
        AvatarEllipse.Visibility = Visibility.Visible;
        AvatarIcon.Visibility = Visibility.Visible;
    }

    async Task FetchUserInfoAsync()
    {
        try
        {
            var result = await AuthManager.Instance.FetchUserInfoAsync();
            if (result.Success)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    UsernameText.Text = AuthManager.Instance.Username;
                    UserIdText.Text = $"ID: {AuthManager.Instance.UserId}";
                    UpdateAvatar();
                    UpdateRank();
                });
            }
        }
        catch { }
    }

    void AvatarButton_Click(object sender, RoutedEventArgs e)
    {
    }

    async void ManageButton_Click(object sender, RoutedEventArgs e)
    {
        NotificationHost.ShowGlobal("请求中...", ToastLevel.Normal);
        try
        {
            var result = await AuthManager.Instance.GenerateUserUrlAsync();
            if (!result.Success)
            {
                NotificationHost.ShowGlobal(result.Message, ToastLevel.Error);
                return;
            }
            if (!string.IsNullOrEmpty(result.UserUrl))
            {
                await Launcher.LaunchUriAsync(new Uri(result.UserUrl));
            }
        }
        catch (Exception ex)
        {
            NotificationHost.ShowGlobal("打开失败: " + ex.Message, ToastLevel.Error);
        }
    }
}
