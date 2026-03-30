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
        
        // 配置 WebSocket 客户端以接受 SSL 证书（仅用于开发环境）
        // 在生产环境中应该使用有效的证书
        System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
    }

    public async Task<bool> ConnectAsync(string serverUrl, string clientToken)
    {
        ClientLogger.LogInfo($"ConnectAsync called with serverUrl: {serverUrl}, clientToken length: {clientToken?.Length ?? 0}");
        
        _serverUrl = serverUrl;
        _clientToken = clientToken;
        
        // 验证服务器 URL 格式
        if (!_serverUrl.StartsWith("wss://") && !_serverUrl.StartsWith("ws://"))
        {
            var errorMessage = "Server URL must start with wss:// or ws://";
            ClientLogger.LogError(errorMessage);
            throw new ArgumentException(errorMessage);
        }

        return await ConnectInternalAsync();
    }

    private async Task<bool> ConnectInternalAsync()
    {
        ClientLogger.LogInfo("ConnectInternalAsync started");
        UpdateConnectionState(ConnectionState.Connecting);

        try
        {
            _webSocket?.Dispose();
            _webSocket = new ClientWebSocket();
            _cts = new CancellationTokenSource();
            
            // 配置 WebSocket 客户端以接受 SSL 证书（仅用于开发环境）
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            
            // 连接到中间件 WebSocket
            var wsUrl = $"{_serverUrl}/ws";
            ClientLogger.LogInfo($"Connecting to WebSocket URL: {wsUrl}");
            
            await _webSocket.ConnectAsync(new Uri(wsUrl), _cts.Token);
            
            ClientLogger.LogInfo("WebSocket connection established successfully");
            _isConnected = true;
            _reconnectAttempts = 0;
            _isReconnecting = false;
            
            // 执行认证流程
            ClientLogger.LogInfo("Starting authentication process");
            var authSuccess = await AuthenticateAsync();
            if (!authSuccess)
            {
                ClientLogger.LogError("Authentication failed");
                _isConnected = false;
                UpdateConnectionState(ConnectionState.Failed);
                return false;
            }
            
            ClientLogger.LogInfo("Authentication successful, connection established");
            UpdateConnectionState(ConnectionState.Connected);
            
            // 启动接收循环和心跳
            _ = ReceiveLoopAsync();
            StartHeartbeat();
            
            return true;
        }
        catch (Exception ex)
        {
            ClientLogger.LogError($"Connection failed: {ex.Message}", ex);
            _isConnected = false;
            UpdateConnectionState(ConnectionState.Failed);
            return false;
        }
    }

    private async Task<bool> AuthenticateAsync()
    {
        try
        {
            ClientLogger.LogInfo("AuthenticateAsync started");
            
            // 生成 Nonce
            var nonce = _cryptoService.GenerateNonce();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            ClientLogger.LogDebug($"Generated nonce: {Convert.ToBase64String(nonce)}, timestamp: {timestamp}");
            
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
            ClientLogger.LogDebug($"Sending auth message: {authJson}");
            
            var authBytes = Encoding.UTF8.GetBytes(authJson);
            await _webSocket!.SendAsync(
                new ArraySegment<byte>(authBytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
            
            ClientLogger.LogInfo("Auth message sent, waiting for response");
            
            // 等待认证结果
            var buffer = new byte[4096];
            var result = await _webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None);
                
            if (result.MessageType != WebSocketMessageType.Text)
            {
                ClientLogger.LogWarning($"Received non-text message type: {result.MessageType}");
                return false;
            }
            
            var responseJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
            ClientLogger.LogInfo($"Auth response received: {responseJson}");
            
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;
            
            if (!root.TryGetProperty("success", out var successElement) || !successElement.GetBoolean())
            {
                var error = root.TryGetProperty("error", out var errorElement) ? errorElement.GetString() : "Authentication failed";
                ClientLogger.LogError($"Authentication failed: {error}");
                return false;
            }
            
            // 获取会话密钥（现在是未加密的 Base64 编码）
            if (root.TryGetProperty("sessionKey", out var sessionKeyElement))
            {
                var sessionKeyBase64 = sessionKeyElement.GetString();
                if (!string.IsNullOrEmpty(sessionKeyBase64))
                {
                    ClientLogger.LogDebug("Decoding session key from Base64");
                    _sessionKey = Convert.FromBase64String(sessionKeyBase64);
                    ClientLogger.LogInfo("Session key decoded successfully");
                }
                else
                {
                    ClientLogger.LogWarning("No session key in auth response");
                }
            }
            else
            {
                ClientLogger.LogWarning("No sessionKey field in auth response");
            }
            
            ClientLogger.LogInfo("Authentication completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            ClientLogger.LogError($"Authentication error: {ex.Message}", ex);
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
        ClientLogger.LogInfo("ReceiveLoopAsync started");
        var buffer = new byte[8192]; // 增加缓冲区大小
        
        try
        {
            while (_isConnected && _webSocket?.State == WebSocketState.Open)
            {
                ClientLogger.LogDebug("Waiting for WebSocket message");
                var result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), 
                    CancellationToken.None);

                ClientLogger.LogDebug($"Received message type: {result.MessageType}, count: {result.Count}");

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    ClientLogger.LogWarning("WebSocket closed by server");
                    _isConnected = false;
                    _ = ReconnectAsync();
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    ClientLogger.LogInfo($"Received message: {json}");
                    
                    try
                    {
                        // 尝试解析为 EncryptedMessage
                        var encryptedMessage = JsonSerializer.Deserialize<EncryptedMessage>(json);
                        
                        if (encryptedMessage != null)
                        {
                            ClientLogger.LogDebug("Processing encrypted message");
                            await ProcessEncryptedMessageAsync(encryptedMessage);
                        }
                        else
                        {
                            ClientLogger.LogDebug("Attempting to parse as ChatMessage");
                            // 可能是直接的 ChatMessage（如错误消息）
                            var chatMessage = JsonSerializer.Deserialize<ChatMessage>(json);
                            if (chatMessage != null)
                            {
                                ClientLogger.LogInfo("Received ChatMessage");
                                MessageReceived?.Invoke(this, chatMessage);
                            }
                            else
                            {
                                ClientLogger.LogWarning($"Could not parse message as either EncryptedMessage or ChatMessage: {json}");
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        ClientLogger.LogError($"Failed to parse message: {ex.Message}", ex);
                        
                        // 检查是否为心跳响应或其他系统消息
                        if (json.Contains("\"type\":\"heartbeat_ack\"", StringComparison.OrdinalIgnoreCase))
                        {
                            ClientLogger.LogInfo("Heartbeat acknowledged");
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
                        ClientLogger.LogInfo("Treating as system message");
                        MessageReceived?.Invoke(this, systemMessage);
                    }
                }
            }
        }
        catch (WebSocketException ex)
        {
            ClientLogger.LogError($"WebSocket error: {ex.Message}", ex);
            _isConnected = false;
            _ = ReconnectAsync();
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
            ClientLogger.LogInfo("Receive loop cancelled");
        }
        catch (Exception ex)
        {
            ClientLogger.LogError($"Unexpected error in receive loop: {ex.Message}", ex);
            _isConnected = false;
            _ = ReconnectAsync();
        }
        
        ClientLogger.LogInfo("ReceiveLoopAsync ended");
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