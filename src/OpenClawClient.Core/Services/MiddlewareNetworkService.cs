using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using OpenClawClient.Core.Models;

namespace OpenClawClient.Core.Services;

/// <summary>
/// 中间件网络服务实现 - 支持加密 WebSocket 通信
/// </summary>
public class MiddlewareNetworkService : INetworkService
{
    private ClientWebSocket? _webSocket;
    private HttpClient _httpClient = new();
    private string _serverUrl = string.Empty;
    private string _clientToken = string.Empty;
    private string _clientId = "client-desktop-001";
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

    // 加密相关
    private byte[]? _sessionKey;
    private readonly IMiddlewareCryptoService _cryptoService;
    
    // 心跳相关
    private CancellationTokenSource? _heartbeatCts;
    private const int HeartbeatIntervalSeconds = 30;

    public bool IsConnected => _isConnected && _webSocket?.State == WebSocketState.Open;

    public event EventHandler<ChatMessage>? MessageReceived;
    public event EventHandler<ConnectionState>? ConnectionStateChanged;

    public MiddlewareNetworkService(IMiddlewareCryptoService cryptoService)
    {
        _cryptoService = cryptoService;
    }

    public async Task<bool> ConnectAsync(string serverUrl, string clientToken)
    {
        _serverUrl = serverUrl;
        _clientToken = clientToken;
        
        // 验证服务器 URL 格式
        if (!_serverUrl.StartsWith("wss://") && !_serverUrl.StartsWith("ws://"))
        {
            throw new ArgumentException("Server URL must start with wss:// or ws://");
        }

        return await ConnectInternalAsync();
    }

    private async Task<bool> ConnectInternalAsync()
    {
        UpdateConnectionState(ConnectionState.Connecting);

        try
        {
            _webSocket?.Dispose();
            _webSocket = new ClientWebSocket();
            _cts = new CancellationTokenSource();
            
            // 连接到中间件 WebSocket
            var wsUrl = $"{_serverUrl}/ws";
            await _webSocket.ConnectAsync(new Uri(wsUrl), _cts.Token);
            
            _isConnected = true;
            _reconnectAttempts = 0;
            _isReconnecting = false;
            
            // 执行认证流程
            var authSuccess = await AuthenticateAsync();
            if (!authSuccess)
            {
                _isConnected = false;
                UpdateConnectionState(ConnectionState.Failed);
                return false;
            }
            
            UpdateConnectionState(ConnectionState.Connected);
            
            // 启动接收循环和心跳
            _ = ReceiveLoopAsync();
            StartHeartbeat();
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MiddlewareNetworkService] Connection failed: {ex.Message}");
            _isConnected = false;
            UpdateConnectionState(ConnectionState.Failed);
            return false;
        }
    }

    private async Task<bool> AuthenticateAsync()
    {
        try
        {
            // 生成 Nonce
            var nonce = _cryptoService.GenerateNonce();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            // 构建认证消息
            var authMessage = new EncryptedMessage
            {
                Type = "auth",
                ClientId = _clientId,
                Token = _clientToken,
                Timestamp = timestamp,
                Nonce = Convert.ToBase64String(nonce)
            };
            
            // 发送认证消息
            var authJson = JsonSerializer.Serialize(authMessage);
            var authBytes = Encoding.UTF8.GetBytes(authJson);
            await _webSocket!.SendAsync(
                new ArraySegment<byte>(authBytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
            
            // 等待认证结果
            var buffer = new byte[4096];
            var result = await _webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None);
                
            if (result.MessageType != WebSocketMessageType.Text)
            {
                return false;
            }
            
            var responseJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"[MiddlewareNetworkService] Auth response: {responseJson}");
            
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;
            
            if (!root.TryGetProperty("success", out var successElement) || !successElement.GetBoolean())
            {
                var error = root.TryGetProperty("error", out var errorElement) ? errorElement.GetString() : "Authentication failed";
                Console.WriteLine($"[MiddlewareNetworkService] Auth failed: {error}");
                return false;
            }
            
            // 获取会话密钥
            if (root.TryGetProperty("encryptedSessionKey", out var sessionKeyElement))
            {
                var encryptedSessionKey = sessionKeyElement.GetString();
                if (!string.IsNullOrEmpty(encryptedSessionKey))
                {
                    _sessionKey = await _cryptoService.ImportSessionKeyAsync(encryptedSessionKey);
                    Console.WriteLine("[MiddlewareNetworkService] Session key imported successfully");
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MiddlewareNetworkService] Authentication error: {ex.Message}");
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        _heartbeatCts?.Cancel();
        _cts?.Cancel();
        
        if (_webSocket?.State == WebSocketState.Open)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
        
        _webSocket?.Dispose();
        _webSocket = null;
        _isConnected = false;
        _sessionKey = null;
        
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
        if (!IsConnected || _sessionKey == null)
            throw new InvalidOperationException("Not connected or session key not available");

        var json = JsonSerializer.Serialize(message);
        var plaintext = Encoding.UTF8.GetBytes(json);
        
        // 加密消息
        var nonce = _cryptoService.GenerateNonce();
        var (encryptedData, tag) = await _cryptoService.EncryptAsync(plaintext, _sessionKey, nonce);
        
        // 构建加密消息
        var encryptedMessage = new EncryptedMessage
        {
            Type = "chat",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Nonce = Convert.ToBase64String(nonce),
            Payload = Convert.ToBase64String(encryptedData),
            Tag = Convert.ToBase64String(tag)
        };
        
        var messageJson = JsonSerializer.Serialize(encryptedMessage);
        var messageBytes = Encoding.UTF8.GetBytes(messageJson);
        
        await _webSocket!.SendAsync(
            new ArraySegment<byte>(messageBytes), 
            WebSocketMessageType.Text, 
            true, 
            CancellationToken.None);
    }

    public async Task<byte[]> DownloadFileAsync(string fileUrl)
    {
        // 文件下载仍然使用 HTTPS
        var url = fileUrl.StartsWith("http") ? fileUrl : $"https:{fileUrl}";
        return await _httpClient.GetByteArrayAsync(url);
    }

    public async Task<string> UploadFileAsync(byte[] fileData, string fileName)
    {
        // 文件上传仍然使用 HTTPS
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(fileData), "file", fileName);
        
        var response = await _httpClient.PostAsync($"{_serverUrl.Replace("wss://", "https://").Replace("ws://", "http://")}/files", content);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadAsStringAsync();
        return result;
    }

    private async Task ReceiveLoopAsync()
    {
        var buffer = new byte[8192]; // 增加缓冲区大小
        
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
                    Console.WriteLine($"[MiddlewareNetworkService] Received: {json}");
                    
                    try
                    {
                        // 尝试解析为 EncryptedMessage
                        var encryptedMessage = JsonSerializer.Deserialize<EncryptedMessage>(json);
                        
                        if (encryptedMessage != null)
                        {
                            await ProcessEncryptedMessageAsync(encryptedMessage);
                        }
                        else
                        {
                            // 可能是直接的 ChatMessage（如错误消息）
                            var chatMessage = JsonSerializer.Deserialize<ChatMessage>(json);
                            if (chatMessage != null)
                            {
                                MessageReceived?.Invoke(this, chatMessage);
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"[MiddlewareNetworkService] Failed to parse message: {ex.Message}");
                        
                        // 检查是否为心跳响应或其他系统消息
                        if (json.Contains("\"type\":\"heartbeat_ack\"", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("[MiddlewareNetworkService] Heartbeat acknowledged");
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
            Console.WriteLine($"[MiddlewareNetworkService] WebSocket error: {ex.Message}");
            _isConnected = false;
            _ = ReconnectAsync();
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
            Console.WriteLine("[MiddlewareNetworkService] Receive loop cancelled");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MiddlewareNetworkService] Unexpected error: {ex.Message}");
            _isConnected = false;
            _ = ReconnectAsync();
        }
    }

    private async Task ProcessEncryptedMessageAsync(EncryptedMessage encryptedMessage)
    {
        if (_sessionKey == null)
        {
            Console.WriteLine("[MiddlewareNetworkService] Received encrypted message but no session key");
            return;
        }
        
        try
        {
            // 验证时间戳
            if (!_cryptoService.ValidateTimestamp(encryptedMessage.Timestamp))
            {
                Console.WriteLine("[MiddlewareNetworkService] Invalid timestamp in encrypted message");
                return;
            }
            
            // 验证 Nonce
            if (!string.IsNullOrEmpty(encryptedMessage.Nonce))
            {
                var nonce = Convert.FromBase64String(encryptedMessage.Nonce!);
                if (_cryptoService.IsNonceUsed(nonce))
                {
                    Console.WriteLine("[MiddlewareNetworkService] Replay attack detected - nonce already used");
                    return;
                }
                _cryptoService.MarkNonceAsUsed(nonce);
            }
            
            // 解密消息
            if (!string.IsNullOrEmpty(encryptedMessage.Payload) && !string.IsNullOrEmpty(encryptedMessage.Tag))
            {
                var encryptedData = Convert.FromBase64String(encryptedMessage.Payload!);
                var tag = Convert.FromBase64String(encryptedMessage.Tag!);
                var nonce = !string.IsNullOrEmpty(encryptedMessage.Nonce) ? 
                    Convert.FromBase64String(encryptedMessage.Nonce!) : 
                    new byte[12]; // 默认 nonce
                
                var plaintext = await _cryptoService.DecryptAsync(encryptedData, _sessionKey, nonce, tag);
                var decryptedJson = Encoding.UTF8.GetString(plaintext);
                
                Console.WriteLine($"[MiddlewareNetworkService] Decrypted message: {decryptedJson}");
                
                // 解析为 ChatMessage
                var chatMessage = JsonSerializer.Deserialize<ChatMessage>(decryptedJson);
                if (chatMessage != null)
                {
                    MessageReceived?.Invoke(this, chatMessage);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MiddlewareNetworkService] Error processing encrypted message: {ex.Message}");
        }
    }

    private void UpdateConnectionState(ConnectionState state)
    {
        ConnectionStateChanged?.Invoke(this, state);
    }
    
    private void StartHeartbeat()
    {
        _heartbeatCts?.Cancel();
        _heartbeatCts = new CancellationTokenSource();
        
        _ = Task.Run(async () =>
        {
            while (!_heartbeatCts.Token.IsCancellationRequested)
            {
                try
                {
                    if (IsConnected)
                    {
                        var nonce = _cryptoService.GenerateNonce();
                        var heartbeatMessage = new EncryptedMessage
                        {
                            Type = "heartbeat",
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            Nonce = Convert.ToBase64String(nonce)
                        };
                        
                        var json = JsonSerializer.Serialize(heartbeatMessage);
                        var bytes = Encoding.UTF8.GetBytes(json);
                        
                        await _webSocket!.SendAsync(
                            new ArraySegment<byte>(bytes),
                            WebSocketMessageType.Text,
                            true,
                            _heartbeatCts.Token);
                            
                        Console.WriteLine("[MiddlewareNetworkService] Sent heartbeat");
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MiddlewareNetworkService] Error sending heartbeat: {ex.Message}");
                }
                
                await Task.Delay(TimeSpan.FromSeconds(HeartbeatIntervalSeconds), _heartbeatCts.Token);
            }
        }, _heartbeatCts.Token);
    }
}