using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenClawClient.Core.Models;
using OpenClawClient.Core.Services;

namespace OpenClawClient.UI.Views;

/// <summary>
/// ChatWindow.xaml 的交互逻辑 - Phase 2 增强版
/// </summary>
public partial class ChatWindow : Window
{
    private readonly LoginConfig _config;
    private readonly INetworkService _networkService;
    private readonly ICryptoService _cryptoService = new CryptoService();
    private bool _isConnected = false;

    public ChatWindow(LoginConfig config, INetworkService networkService)
    {
        InitializeComponent();
        _config = config;
        _networkService = networkService;
        
        // 订阅事件
        _networkService.MessageReceived += OnMessageReceived;
        _networkService.ConnectionStateChanged += OnConnectionStateChanged;
        
        // 允许拖拽
        MessagesListBox.AllowDrop = true;
        
        // 粘贴事件
        DataObject.AddPastingHandler(InputTextBox, OnPaste);
        
        // 初始化连接状态
        UpdateConnectionStatus(ConnectionState.Connecting);
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
            Timestamp = DateTime.Now,
            IsEncrypted = true,
            Status = DeliveryStatus.Pending
        };

        // 加密消息内容
        if (!string.IsNullOrEmpty(_config.AesKey))
        {
            message.Content = _cryptoService.Encrypt(content, _config.AesKey);
        }

        try
        {
            await _networkService.SendMessageAsync(message);
            message.Status = DeliveryStatus.Sent;
            
            // 添加到消息列表（本地显示）
            AddMessageToUI(message);
            InputTextBox.Clear();
        }
        catch (Exception ex)
        {
            message.Status = DeliveryStatus.Failed;
            AddMessageToUI(message);
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

        AddMessageToUI(message);
    }

    private void AddMessageToUI(ChatMessage message)
    {
        Dispatcher.Invoke(() =>
        {
            // 设置消息气泡样式
            var border = new Border
            {
                Margin = new Thickness(5, 3, 5, 3),
                Padding = new Thickness(12, 8, 12, 8),
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 211, 211, 211)), // Light gray
                BorderThickness = new Thickness(1, 1, 1, 1),
                MaxWidth = 600
            };

            // 根据角色设置背景和对齐
            if (message.Role == MessageRole.User)
            {
                border.Background = new SolidColorBrush(Color.FromArgb(255, 232, 224, 250)); // Light purple
                border.HorizontalAlignment = HorizontalAlignment.Right;
            }
            else if (message.Role == MessageRole.System)
            {
                border.Background = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240)); // Light gray
                border.HorizontalAlignment = HorizontalAlignment.Center;
            }
            else
            {
                border.Background = new SolidColorBrush(Colors.White);
                border.HorizontalAlignment = HorizontalAlignment.Left;
            }

            var stackPanel = new StackPanel();

            // 文件/图片预览
            if (message.Type == MessageType.File || message.Type == MessageType.Image)
            {
                var fileStack = new StackPanel();
                var fileNameText = new TextBlock
                {
                    Text = message.FileName ?? "",
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                fileStack.Children.Add(fileNameText);
                stackPanel.Children.Add(fileStack);
            }

            // 消息内容
            var contentText = new TextBlock
            {
                Text = message.Content,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14
            };
            stackPanel.Children.Add(contentText);

            // 时间戳和状态
            var statusStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 5, 0, 0)
            };
            var timestampText = new TextBlock
            {
                Text = message.Timestamp.ToString("HH:mm"),
                FontSize = 10,
                Foreground = new SolidColorBrush(Colors.Gray)
            };
            statusStack.Children.Add(timestampText);

            if (message.IsEncrypted)
            {
                var lockText = new TextBlock { Text = " 🔒", FontSize = 10 };
                statusStack.Children.Add(lockText);
            }

            var statusText = new TextBlock
            {
                FontSize = 10,
                Margin = new Thickness(5, 0, 0, 0)
            };
            statusText.Text = message.Status switch
            {
                DeliveryStatus.Pending => "⏳",
                DeliveryStatus.Sent => "✓",
                DeliveryStatus.Delivered => "✓✓",
                DeliveryStatus.Failed => "✕",
                _ => ""
            };
            statusStack.Children.Add(statusText);

            stackPanel.Children.Add(statusStack);
            border.Child = stackPanel;

            MessagesListBox.Items.Add(border);
            MessagesListBox.ScrollIntoView(border);
        });
    }

    private void OnConnectionStateChanged(object? sender, ConnectionState state)
    {
        _isConnected = (state == ConnectionState.Connected);
        UpdateConnectionStatus(state);
    }

    private void UpdateConnectionStatus(ConnectionState state)
    {
        Dispatcher.Invoke(() =>
        {
            var statusText = state switch
            {
                ConnectionState.Disconnected => "● 已断开",
                ConnectionState.Connecting => "○ 连接中...",
                ConnectionState.Connected => "● 已连接",
                ConnectionState.Reconnecting => "○ 重连中...",
                ConnectionState.Failed => "✕ 连接失败",
                _ => "? 未知"
            };
            
            Title = $"OpenClaw Client - {statusText}";
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

    private void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        // 处理粘贴
        _ = HandlePasteAsync();
    }

    private async Task HandlePasteAsync()
    {
        try
        {
            if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                foreach (string file in files)
                {
                    await SendFileAsync(file);
                }
            }
            else if (Clipboard.ContainsImage())
            {
                var image = Clipboard.GetImage();
                if (image != null)
                {
                    await SendImageAsync(image);
                }
            }
            else if (Clipboard.ContainsText())
            {
                InputTextBox.Text = Clipboard.GetText();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"粘贴失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task SendFileAsync(string filePath)
    {
        if (!_isConnected)
        {
            MessageBox.Show("未连接到服务器，无法发送文件", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var fileName = Path.GetFileName(filePath);
        var message = new ChatMessage
        {
            Content = $"📎 发送文件：{fileName}",
            Type = MessageType.File,
            Role = MessageRole.User,
            FileName = fileName,
            Timestamp = DateTime.Now,
            IsEncrypted = false
        };

        AddMessageToUI(message);

        try
        {
            var fileData = await File.ReadAllBytesAsync(filePath);
            var result = await _networkService.UploadFileAsync(fileData, fileName);
            
            message.FilePath = result;
            message.Status = DeliveryStatus.Sent;
            
            // 更新消息状态（可选：显示上传成功）
        }
        catch (Exception ex)
        {
            message.Status = DeliveryStatus.Failed;
            MessageBox.Show($"发送文件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task SendImageAsync(System.Windows.Media.ImageSource image)
    {
        if (!_isConnected)
        {
            MessageBox.Show("未连接到服务器，无法发送图片", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 将图片保存为临时文件
        var tempFile = Path.Combine(Path.GetTempPath(), $"openclaw_{Guid.NewGuid():N}.png");
        
        try
        {
            var encoder = new PngBitmapEncoder();
            if (image is BitmapSource bitmap)
            {
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
            }

            using (var stream = new FileStream(tempFile, FileMode.Create))
            {
                encoder.Save(stream);
            }

            await SendFileAsync(tempFile);
        }
        finally
        {
            // 清理临时文件
            if (File.Exists(tempFile))
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch { }
            }
        }
    }
}
