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
/// OpenClaw Gateway 网络服务实现
/// </summary>
public class NetworkService : INetworkService
{
    private ClientWebSocket? _webSocket;
    private HttpClient _httpClient = new();
    private string _serverUrl = string.Empty;
    private string _token = string.Empty;
    private CancellationTokenSource? _cts;
    private bool _isConnected;

    public event EventHandler<ChatMessage>? MessageReceived;
    public event EventHandler<ConnectionState>? ConnectionStateChanged;

    public async Task<bool> ConnectAsync(string serverUrl, string token)
    {
        _serverUrl = serverUrl;
        _token = token;
        
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-Gateway-Token", token);

        var wsUrl = serverUrl.Replace("https://", "wss://").Replace("http://", "ws://");
        
        try
        {
            _webSocket = new ClientWebSocket();
            _cts = new CancellationTokenSource();
            
            await _webSocket.ConnectAsync(new Uri($"{wsUrl}/ws"), _cts.Token);
            _isConnected = true;
            
            ConnectionStateChanged?.Invoke(this, ConnectionState.Connected);
            
            _ = ReceiveLoopAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            ConnectionStateChanged?.Invoke(this, ConnectionState.Failed);
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
        
        ConnectionStateChanged?.Invoke(this, ConnectionState.Disconnected);
    }

    public async Task SendMessageAsync(ChatMessage message)
    {
        if (!_isConnected || _webSocket?.State != WebSocketState.Open)
            throw new InvalidOperationException("Not connected");

        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        
        await _webSocket.SendAsync(
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
                    await DisconnectAsync();
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var message = JsonSerializer.Deserialize<ChatMessage>(json);
                    
                    if (message != null)
                    {
                        MessageReceived?.Invoke(this, message);
                    }
                }
            }
        }
        catch (WebSocketException)
        {
            _isConnected = false;
            ConnectionStateChanged?.Invoke(this, ConnectionState.Reconnecting);
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
    }
}
