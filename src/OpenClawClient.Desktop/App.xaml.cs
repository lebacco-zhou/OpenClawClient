using System.Windows;
using OpenClawClient.UI.Converters;

namespace OpenClawClient.Desktop;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // 注册转换器资源
        Resources.Add("RoleToBackgroundConverter", new RoleToBackgroundConverter());
        Resources.Add("BoolToVisibilityConverter", new BoolToVisibilityConverter());
        Resources.Add("RoleToAlignmentConverter", new RoleToAlignmentConverter());
        Resources.Add("FileTypeToVisibilityConverter", new FileTypeToVisibilityConverter());
        Resources.Add("StatusToIconConverter", new StatusToIconConverter());
        Resources.Add("ConnectionStateToTextConverter", new ConnectionStateToTextConverter());
        Resources.Add("MessageTypeToIconConverter", new MessageTypeToIconConverter());
    }
}
