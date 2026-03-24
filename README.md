# OpenClaw Client

安全加密的 Windows 桌面聊天客户端

## 📦 功能特性

- ✅ 端到端加密通信（AES-256-GCM）
- ✅ 登录配置管理（服务器地址、Gateway Token）
- ✅ 文件拖拽发送
- ✅ 剪贴板粘贴图片/文件
- ✅ 自定义文件下载路径
- ✅ 自动日期子文件夹分类
- ✅ 绿色版 + 安装包版双版本
- ✅ 连接状态指示器
- ✅ 消息发送状态跟踪
- ✅ 文件下载进度显示

## 🛠️ 技术栈

- .NET 8.0
- WPF
- MaterialDesignInXaml
- AES-256-GCM 加密
- DPAPI 本地数据保护

## 📁 项目结构

```
OpenClawClient/
├── src/
│   ├── OpenClawClient.Core/      # 核心业务逻辑
│   ├── OpenClawClient.UI/        # WPF 界面
│   └── OpenClawClient.Desktop/   # 桌面应用入口
├── tests/                        # 单元测试
├── scripts/                      # 发布脚本
│   ├── publish-portable.ps1      # 绿色版发布
│   └── publish-setup.ps1         # 安装包发布
├── installer/                    # Inno Setup 脚本
└── dist/                         # 输出文件
```

## 🚀 开发指南

### 环境要求

- Windows 10/11
- .NET 8.0 SDK
- Visual Studio 2022 (可选)
- Inno Setup 6 (安装包编译)

### 本地运行

```bash
cd OpenClawClient
dotnet run --project src/OpenClawClient.Desktop
```

### 发布绿色版

```powershell
.\scripts\publish-portable.ps1
```

输出：`dist/OpenClawClient_Portable.zip`

### 发布安装包版

```powershell
.\scripts\publish-setup.ps1
```

输出：`dist/OpenClawClient_Setup.exe`

## 📝 配置文件

### 绿色版配置路径
```
./config/appsettings.json
```

### 安装包版配置路径
```
%APPDATA%\OpenClawClient\appsettings.json
```

### 配置示例
```json
{
  "serverUrl": "https://www.lebacco.cn:8443",
  "gatewayToken": "<31 位十六进制令牌>",
  "aesKey": "<AES-256 密钥>",
  "downloadPath": "D:\\MyFiles\\OpenClaw",
  "autoSubfolder": true,
  "rememberLogin": true
}
```

## 🔒 安全说明

- 敏感字段使用 DPAPI 加密存储
- 消息内容使用 AES-256-GCM 端到端加密
- 文件传输使用 HTTPS 加密通道
- 不存储明文密码或令牌

## 📄 许可证

MIT-0
