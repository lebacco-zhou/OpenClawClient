using System.Windows;

namespace OpenClawClient.Desktop;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // 不再注册转换器资源，因为聊天界面现在使用代码动态创建 UI
        // 所有转换器依赖已在 ChatWindow.xaml.cs 中移除
    }
}