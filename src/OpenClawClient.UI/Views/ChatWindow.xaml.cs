using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OpenClawClient.Core.Models;
using OpenClawClient.Core.Services;

namespace OpenClawClient.UI.Views;

/// <summary>
/// ChatWindow.xaml 的交互逻辑
/// </summary>
public partial class ChatWindow : Window
{
    private readonly LoginConfig _config;
    private readonly INetworkService _networkService;
    private readonly ICryptoService _cryptoService = new CryptoService();

    public ChatWindow(LoginConfig config, INetworkService networkService)
    {
        InitializeComponent();
        _config = config;
        _networkService = networkService;
        
        // 订阅消息接收事件
        _networkService.MessageReceived += OnMessageReceived;
        _networkService.ConnectionStateChanged += OnConnectionStateChanged;
        
        // 允许拖拽
        MessagesListBox.AllowDrop = true;
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        await SendMessageAsync();
    }

    private async Task SendMessageAsync()
    {
        var content = InputTextBox.Text.Trim();
        if (string.IsNullOrEmpty(content))
            return;

        var message = new ChatMessage
        {
            Content = content,
            Type = MessageType.Text,
            Role = MessageRole.User,
            IsEncrypted = true
        };

        // 加密消息内容
        if (!string.IsNullOrEmpty(_config.AesKey))
        {
            message.Content = _cryptoService.Encrypt(content, _config.AesKey);
        }

        try
        {
            await _networkService.SendMessageAsync(message);
            InputTextBox.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"发送失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl+Enter 发送
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _ = SendMessageAsync();
            e.Handled = true;
        }
    }

    private void OnMessageReceived(object? sender, ChatMessage message)
    {
        // 解密消息内容
        if (!string.IsNullOrEmpty(_config.AesKey) && message.IsEncrypted)
        {
            try
            {
                message.Content = _cryptoService.Decrypt(message.Content, _config.AesKey);
            }
            catch
            {
                // 解密失败，显示原始内容
            }
        }

        // 更新 UI（需要在 UI 线程）
        Dispatcher.Invoke(() =>
        {
            MessagesListBox.Items.Add(message);
            MessagesListBox.ScrollIntoView(message);
        });
    }

    private void OnConnectionStateChanged(object? sender, ConnectionState state)
    {
        Dispatcher.Invoke(() =>
        {
            Title = $"OpenClaw Client - {state}";
        });
    }

    private void MessagesListBox_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) 
            ? DragDropEffects.Copy 
            : DragDropEffects.None;
        e.Handled = true;
    }

    private async void MessagesListBox_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            
            foreach (var file in files)
            {
                await SendFileAsync(file);
            }
        }
    }

    private async Task SendFileAsync(string filePath)
    {
        try
        {
            var fileData = await File.ReadAllBytesAsync(filePath);
            var fileName = Path.GetFileName(filePath);
            
            // 上传文件
            var result = await _networkService.UploadFileAsync(fileData, fileName);
            
            // 发送文件消息
            var message = new ChatMessage
            {
                Content = $"发送文件：{fileName}",
                Type = MessageType.File,
                Role = MessageRole.User,
                FileName = fileName,
                FilePath = result,
                IsEncrypted = false // 文件本身已加密传输
            };

            await _networkService.SendMessageAsync(message);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"发送文件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
