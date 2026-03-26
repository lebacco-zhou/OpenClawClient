using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using OpenClawClient.Core.Models;

namespace OpenClawClient.Core.Services;

/// <summary>
/// 网络服务接口
/// </summary>
public interface INetworkService
{
    Task<bool> ConnectAsync(string serverUrl, string token);
    Task DisconnectAsync();
    Task SendMessageAsync(ChatMessage message);
    Task<byte[]> DownloadFileAsync(string fileUrl);
    Task<string> UploadFileAsync(byte[] fileData, string fileName);
    event EventHandler<ChatMessage>? MessageReceived;
    event EventHandler<ConnectionState>? ConnectionStateChanged;
    bool IsConnected { get; }
    Task ReconnectAsync();
}

public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Failed
}

/// <summary>
/// OpenClaw Gateway 网络服务实现 - 增强自动重连
/// </summary>
public class NetworkService : INetworkService
{
    private ClientWebSocket? _webSocket;
    private HttpClient _httpClient = new();
    private string _serverUrl = string.Empty;
    private string _token = string.Empty;
    private CancellationTokenSource? _cts;
    private bool _isConnected;
    private bool _isReconnecting;
    private int _reconnectAttempts = 0;
    private const int MaxReconnectAttempts = 5;
    private static readonly TimeSpan[] ReconnectDelays = 
    {
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30)
    };

    public bool IsConnected => _isConnected && _webSocket?.State == WebSocketState.Open;

    public event EventHandler<ChatMessage>? MessageReceived;
    public event EventHandler<ConnectionState>? ConnectionStateChanged;

    public async Task<bool> ConnectAsync(string serverUrl, string token)
    {
        _serverUrl = serverUrl;
        _token = token;
        
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-Gateway-Token", token);

        return await ConnectInternalAsync();
    }

    private async Task<bool> ConnectInternalAsync()
    {
        UpdateConnectionState(ConnectionState.Connecting);

        var wsUrl = _serverUrl.Replace("https://", "wss://").Replace("http://", "ws://");
        
        try
        {
            _webSocket?.Dispose();
            _webSocket = new ClientWebSocket();
            _cts = new CancellationTokenSource();
            
            await _webSocket.ConnectAsync(new Uri($"{wsUrl}/ws"), _cts.Token);
            _isConnected = true;
            _reconnectAttempts = 0;
            _isReconnecting = false;
            
            UpdateConnectionState(ConnectionState.Connected);
            
            _ = ReceiveLoopAsync();
            
            return true;
        }
        catch
        {
            _isConnected = false;
            UpdateConnectionState(ConnectionState.Failed);
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        _cts?.Cancel();
        
        if (_webSocket?.State == WebSocketState.Open)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
        
        _webSocket?.Dispose();
        _webSocket = null;
        _isConnected = false;
        
        UpdateConnectionState(ConnectionState.Disconnected);
    }

    public async Task ReconnectAsync()
    {
        if (_isReconnecting)
            return;

        _isReconnecting = true;

        while (_reconnectAttempts < MaxReconnectAttempts && !_isConnected)
        {
            var delay = _reconnectAttempts < ReconnectDelays.Length 
                ? ReconnectDelays[_reconnectAttempts] 
                : ReconnectDelays[^1];

            UpdateConnectionState(ConnectionState.Reconnecting);
            await Task.Delay(delay);

            _reconnectAttempts++;
            var success = await ConnectInternalAsync();

            if (success)
            {
                _isReconnecting = false;
                return;
            }
        }

        _isReconnecting = false;
        UpdateConnectionState(ConnectionState.Failed);
    }

    public async Task SendMessageAsync(ChatMessage message)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Not connected");

        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        
        await _webSocket!.SendAsync(
            new ArraySegment<byte>(bytes), 
            WebSocketMessageType.Text, 
            true, 
            CancellationToken.None);
    }

    public async Task<byte[]> DownloadFileAsync(string fileUrl)
    {
        var url = fileUrl.StartsWith("http") ? fileUrl : $"{_serverUrl}{fileUrl}";
        return await _httpClient.GetByteArrayAsync(url);
    }

    public async Task<string> UploadFileAsync(byte[] fileData, string fileName)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(fileData), "file", fileName);
        
        var response = await _httpClient.PostAsync($"{_serverUrl}/files", content);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadAsStringAsync();
        return result;
    }

    private async Task ReceiveLoopAsync()
    {
        var buffer = new byte[4096];
        
        try
        {
            while (_isConnected && _webSocket?.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), 
                    CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _isConnected = false;
                    _ = ReconnectAsync();
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"[NetworkService] Received: {json}");
                    
                    try
                    {
                        // 尝试解析为 ChatMessage
                        var message = JsonSerializer.Deserialize<ChatMessage>(json);
                        
                        if (message != null)
                        {
                            Console.WriteLine($"[NetworkService] Message parsed: Role={message.Role}, Type={message.Type}, Content='{message.Content}'");
                            
                            // 过滤心跳或系统管理消息
                            if (ShouldFilterMessage(message, json))
                            {
                                Console.WriteLine($"[NetworkService] Filtering system message: {json}");
                                continue;
                            }
                            
                            MessageReceived?.Invoke(this, message);
                        }
                        else
                        {
                            Console.WriteLine("[NetworkService] Message deserialized to null");
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"[NetworkService] Failed to parse message: {ex.Message}");
                        
                        // 检查是否为心跳或其他系统消息
                        if (ShouldFilterSystemMessage(json))
                        {
                            Console.WriteLine($"[NetworkService] Filtering system message: {json}");
                            continue;
                        }
                        
                        // 解析为系统消息
                        var systemMessage = new ChatMessage
                        {
                            Content = json,
                            Role = MessageRole.System,
                            Type = MessageType.Text,
                            Timestamp = DateTime.Now,
                            IsEncrypted = false
                        };
                        MessageReceived?.Invoke(this, systemMessage);
                    }
                }
            }
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine($"[NetworkService] WebSocket error: {ex.Message}");
            _isConnected = false;
            _ = ReconnectAsync();
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
            Console.WriteLine("[NetworkService] Receive loop cancelled");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkService] Unexpected error: {ex.Message}");
            _isConnected = false;
            _ = ReconnectAsync();
        }
    }

    private void UpdateConnectionState(ConnectionState state)
    {
        ConnectionStateChanged?.Invoke(this, state);
    }
    
    /// <summary>
    /// 检查是否应该过滤此消息（心跳、系统管理消息等）
    /// </summary>
    private bool ShouldFilterMessage(ChatMessage message, string originalJson)
    {
        // 过滤空消息
        if (string.IsNullOrWhiteSpace(message.Content) && 
            message.Type == MessageType.Text && 
            message.Role == MessageRole.System)
        {
            return true;
        }
        
        // 检查是否为心跳消息（通常包含特定关键词）
        if (originalJson.Contains("\"type\":\"heartbeat\"", StringComparison.OrdinalIgnoreCase) ||
            originalJson.Contains("\"type\":\"ping\"", StringComparison.OrdinalIgnoreCase) ||
            originalJson.Contains("\"type\":\"pong\"", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        // 检查是否为连接状态消息
        if (originalJson.Contains("\"connected\":", StringComparison.OrdinalIgnoreCase) ||
            originalJson.Contains("\"disconnected\":", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 检查是否应该过滤系统消息
    /// </summary>
    private bool ShouldFilterSystemMessage(string json)
    {
        // 过滤心跳消息
        if (json.Contains("\"type\":\"heartbeat\"", StringComparison.OrdinalIgnoreCase) ||
            json.Contains("\"type\":\"ping\"", StringComparison.OrdinalIgnoreCase) ||
            json.Contains("\"type\":\"pong\"", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        // 过滤连接状态消息
        if (json.Contains("\"connected\":", StringComparison.OrdinalIgnoreCase) ||
            json.Contains("\"disconnected\":", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        return false;
    }
}
