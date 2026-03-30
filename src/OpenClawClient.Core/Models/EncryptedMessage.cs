using System.Text.Json.Serialization;

namespace OpenClawClient.Core.Models;

/// <summary>
/// 加密消息格式 - 与中间件协议匹配
/// </summary>
public class EncryptedMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
    
    [JsonPropertyName("nonce")]
    public string? Nonce { get; set; }
    
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }
    
    [JsonPropertyName("tag")]
    public string? Tag { get; set; }
}