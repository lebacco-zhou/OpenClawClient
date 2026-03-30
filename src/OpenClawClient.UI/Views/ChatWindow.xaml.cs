using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
    private string _selectedModel; // 存储选中的模型

    public ChatWindow(LoginConfig config, INetworkService networkService)
    {
        InitializeComponent();
        _config = config;
        _networkService = networkService;
        _selectedModel = config.SelectedModel ?? "qwen3.5-plus";  // 初始化模型选择
        
        // 订阅事件
        _networkService.MessageReceived += OnMessageReceived;
        _networkService.ConnectionStateChanged += OnConnectionStateChanged;
        
        // 允许拖拽
        MessagesListBox.AllowDrop = true;
        
        // 粘贴事件
        DataObject.AddPastingHandler(InputTextBox, OnPaste);
        
        // 初始化连接状态
        UpdateConnectionStatus(ConnectionState.Connecting);
        
        // 设置模型选择
        SetModelSelection(_selectedModel);
        
        // 在标题中显示模型信息
        Title += $" - [{_selectedModel}]";
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
        // 过滤掉空消息或系统心跳消息
        if (string.IsNullOrWhiteSpace(message.Content) && 
            message.Type != MessageType.File && 
            message.Type != MessageType.Image)
        {
            Console.WriteLine($"[ChatWindow] Filtered empty message: Role={message.Role}, Type={message.Type}");
            return;
        }

        // 解密消息内容
        if (!string.IsNullOrEmpty(_config.AesKey) && message.IsEncrypted)
        {
            try
            {
                message.Content = _cryptoService.Decrypt(message.Content, _config.AesKey);
            }
            catch (Exception ex)
            {
                // 解密失败，显示原始内容和错误信息
                Console.WriteLine($"解密失败：{ex.Message}");
                // 不修改原始内容，继续显示
            }
        }

        // 过滤掉可能的心跳或其他系统消息
        if (string.IsNullOrWhiteSpace(message.Content) && 
            message.Type == MessageType.Text &&
            message.Role == MessageRole.System)
        {
            // 如果是系统消息但没有内容，则忽略
            if (string.IsNullOrWhiteSpace(message.Content))
            {
                Console.WriteLine($"[ChatWindow] Ignoring empty system message");
                return;
            }
        }

        AddMessageToUI(message);
    }

    private void AddMessageToUI(ChatMessage message)
    {
        Dispatcher.Invoke(() =>
        {
            Console.WriteLine($"[ChatWindow] Adding message: Role={message.Role}, Content={message.Content?.Substring(0, Math.Min(50, message.Content?.Length ?? 0)) ?? "null"}");
            
            // 在代码中动态创建消息气泡 UI
            var messageBorder = CreateMessageBubble(message);
            MessagesListBox.Items.Add(messageBorder);
            MessagesListBox.ScrollIntoView(messageBorder);
            
            Console.WriteLine($"[ChatWindow] Added message to ListBox, item count: {MessagesListBox.Items.Count}");
        });
    }

    /// <summary>
    /// 在代码中动态创建消息气泡 UI（替代 XAML DataTemplate 和转换器）
    /// </summary>
    private Border CreateMessageBubble(ChatMessage message)
    {
        var mainPanel = new StackPanel();
        
        // 文件/图片预览区域
        if (message.Type == MessageType.File || message.Type == MessageType.Image)
        {
            var filePanel = new StackPanel();
            
            // 图片预览
            if (message.Type == MessageType.Image && !string.IsNullOrEmpty(message.FilePath))
            {
                try
                {
                    var image = new Image
                    {
                        Source = new BitmapImage(new Uri(message.FilePath)),
                        MaxHeight = 200,
                        MaxWidth = 400,
                        Stretch = Stretch.Uniform,
                        Margin = new Thickness(0, 0, 0, 8),
                        Cursor = Cursors.Hand
                    };
                    image.MouseLeftButtonUp += async (s, e) =>
                    {
                        if (image.Source is BitmapSource bitmap)
                        {
                            await ShowImageViewerAsync(bitmap);
                        }
                    };
                    filePanel.Children.Add(image);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ChatWindow] Failed to load image: {ex.Message}");
                    // 如果图片加载失败，显示文件名作为文本
                    var fileNameText = new TextBlock
                    {
                        Text = $"🖼️ {message.FileName ?? "图片"}",
                        FontWeight = FontWeights.SemiBold,
                        Foreground = GetResourceBrush("TextPrimaryBrush"),
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    filePanel.Children.Add(fileNameText);
                }
            }
            
            // 文件名
            if (!string.IsNullOrEmpty(message.FileName))
            {
                var fileNameText = new TextBlock
                {
                    Text = message.FileName,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = GetResourceBrush("TextPrimaryBrush"),
                    Margin = new Thickness(0, 0, 0, 5)
                };
                filePanel.Children.Add(fileNameText);
            }
            
            mainPanel.Children.Add(filePanel);
        }
        
        // 消息内容
        var contentText = new TextBlock
        {
            Text = message.Content,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            LineHeight = 20
        };
        
        // 根据角色设置颜色
        contentText.Foreground = message.Role switch
        {
            MessageRole.User => Brushes.White,
            MessageRole.Assistant => GetResourceBrush("TextPrimaryBrush"),
            MessageRole.System => GetResourceBrush("InfoBrush"),
            _ => GetResourceBrush("TextPrimaryBrush")
        };
        
        if (message.Role == MessageRole.System)
        {
            contentText.FontStyle = FontStyles.Italic;
            contentText.FontSize = 12;
        }
        
        mainPanel.Children.Add(contentText);
        
        // 时间戳和状态
        var statusPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 8, 0, 0)
        };
        
        var timestampText = new TextBlock
        {
            Text = message.Timestamp.ToString("HH:mm"),
            FontSize = 10,
            Foreground = GetResourceBrush("TextSecondaryBrush"),
            Opacity = 0.8
        };
        statusPanel.Children.Add(timestampText);
        
        // 加密图标（如果已加密）
        if (message.IsEncrypted)
        {
            var lockIcon = new Path
            {
                Data = Geometry.Parse("M12,1L3,5V11C3,16.55 6.84,21.74 12,23C17.16,21.74 21,16.55 21,11V5L12,1M12,7C14.21,7 16,8.79 16,11C16,13.21 14.21,15 12,15C9.79,15 8,13.21 8,11C8,8.79 9.79,7 12,7Z"),
                Fill = GetResourceBrush("SuccessBrush"),
                Width = 12,
                Height = 12,
                Margin = new Thickness(6, 0, 0, 0)
            };
            statusPanel.Children.Add(lockIcon);
        }
        
        mainPanel.Children.Add(statusPanel);
        
        // 创建消息边框
        var messageBorder = new Border
        {
            Child = mainPanel,
            Margin = new Thickness(0, 8),
            MaxWidth = 600,
            Padding = new Thickness(12, 10),
            CornerRadius = new CornerRadius(8)
        };
        
        // 根据角色设置背景和对齐
        switch (message.Role)
        {
            case MessageRole.User:
                messageBorder.Background = GetResourceBrush("PrimaryBrush");
                messageBorder.HorizontalAlignment = HorizontalAlignment.Right;
                break;
            case MessageRole.Assistant:
                messageBorder.Background = GetResourceBrush("CardBackgroundBrush");
                messageBorder.HorizontalAlignment = HorizontalAlignment.Left;
                break;
            case MessageRole.System:
                messageBorder.Background = Brushes.Transparent;
                messageBorder.HorizontalAlignment = HorizontalAlignment.Center;
                break;
        }
        
        return messageBorder;
    }

    /// <summary>
    /// 从资源获取画刷
    /// </summary>
    private Brush GetResourceBrush(string key)
    {
        try
        {
            return (Brush)FindResource(key);
        }
        catch
        {
            return Brushes.White;
        }
    }

    /// <summary>
    /// 显示图片查看器
    /// </summary>
    private async Task ShowImageViewerAsync(BitmapSource bitmap)
    {
        try
        {
            var viewer = new OpenClawClient.UI.Views.ImageViewerWindow(bitmap);
            viewer.ShowDialog();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatWindow] Failed to show image viewer: {ex.Message}");
        }
    }

    private void OnConnectionStateChanged(object? sender, ConnectionState state)
    {
        _isConnected = (state == ConnectionState.Connected);
        UpdateConnectionStatus(state);
    }

    private void SetModelSelection(string modelName)
    {
        switch (modelName.ToLower())
        {
            case "qwen3.5-plus":
                ModelComboBox.SelectedIndex = 0;
                break;
            case "qwen3-coder-plus":
                ModelComboBox.SelectedIndex = 1;
                break;
            case "qwen3-coder-max":
                ModelComboBox.SelectedIndex = 2;
                break;
            case "qwen3-max":
                ModelComboBox.SelectedIndex = 3;
                break;
            case "qwen2.5":
                ModelComboBox.SelectedIndex = 4;
                break;
            case "custom-model":
                ModelComboBox.SelectedIndex = 5;
                break;
            default:
                ModelComboBox.SelectedIndex = 0; // 默认 Qwen 3.5 Plus
                break;
        }
    }
    
    private string GetSelectedModel()
    {
        return ModelComboBox.SelectedIndex switch
        {
            0 => "qwen3.5-plus",
            1 => "qwen3-coder-plus", 
            2 => "qwen3-coder-max",
            3 => "qwen3-max",
            4 => "qwen2.5",
            5 => "custom-model",
            _ => "qwen3.5-plus" // 默认
        };
    }
    
    private void ModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedModel = GetSelectedModel();
        Title = Title.Split(" - ")[0] + $" - [{_selectedModel}]"; // 更新窗口标题显示当前模型
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

    private void AttachButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "所有文件|*.*|图片文件|*.jpg;*.jpeg;*.png;*.gif;*.webp|文档文件|*.pdf;*.doc;*.docx;*.xls;*.xlsx;*.ppt;*.pptx",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                _ = SendFileAsync(file);
            }
        }
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        var searchDialog = new OpenClawClient.UI.Views.SearchDialog(MessagesListBox);
        searchDialog.ShowDialog();
    }
}