namespace OpenClawClient.Core.Models;

/// <summary>
/// 聊天消息模型
/// </summary>
public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;
    public MessageRole Role { get; set; } = MessageRole.User;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public bool IsEncrypted { get; set; } = true;
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;
}

public enum MessageType
{
    Text,
    Image,
    File,
    System
}

public enum MessageRole
{
    User,
    Assistant,
    System
}

public enum DeliveryStatus
{
    Pending,
    Sent,
    Delivered,
    Failed
}
