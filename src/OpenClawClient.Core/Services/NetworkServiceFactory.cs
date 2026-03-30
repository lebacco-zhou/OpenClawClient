namespace OpenClawClient.Core.Services;

/// <summary>
/// 网络服务工厂 - 根据配置选择合适的网络服务实现
/// </summary>
public static class NetworkServiceFactory
{
    public static INetworkService CreateNetworkService()
    {
        // 默认使用中间件网络服务
        var cryptoService = new MiddlewareCryptoService();
        return new MiddlewareNetworkService(cryptoService);
    }
    
    /// <summary>
    /// 创建直接连接 Gateway 的网络服务（用于兼容性）
    /// </summary>
    public static INetworkService CreateDirectGatewayService()
    {
        return new NetworkService();
    }
}