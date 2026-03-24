# OpenClaw Client - Windows 构建说明

本文档详细说明如何在 Windows 上构建 OpenClaw Client 应用程序。

---

## 📋 目录

1. [环境要求](#环境要求)
2. [安装步骤](#安装步骤)
3. [克隆项目](#克隆项目)
4. [本地调试](#本地调试)
5. [发布绿色版](#发布绿色版)
6. [发布安装包](#发布安装包)
7. [验证构建](#验证构建)
8. [故障排查](#故障排查)

---

## 🖥️ 环境要求

### 必需软件

| 软件 | 版本 | 下载地址 | 大小 |
|------|------|----------|------|
| **.NET 8.0 SDK** | 8.0.x | https://dotnet.microsoft.com/download/dotnet/8.0 | ~200MB |
| **Git** | 2.x+ | https://git-scm.com/download/win | ~50MB |
| **Windows** | 10/11 (64 位) | - | - |

### 可选软件

| 软件 | 用途 | 下载地址 |
|------|------|----------|
| **Visual Studio 2022** | IDE 开发环境 | https://visualstudio.microsoft.com/ |
| **Inno Setup 6** | 编译安装包 | https://jrsoftware.org/isdl.php |
| **VS Code** | 轻量级编辑器 | https://code.visualstudio.com/ |

---

## 📥 安装步骤

### 步骤 1: 安装 .NET 8.0 SDK

1. 访问 https://dotnet.microsoft.com/download/dotnet/8.0
2. 下载 **SDK 8.0.x** (选择 x64 版本)
3. 运行安装程序，按提示完成安装
4. 验证安装：
   ```cmd
   dotnet --version
   ```
   应显示类似 `8.0.100` 的版本号

### 步骤 2: 安装 Git

1. 访问 https://git-scm.com/download/win
2. 下载并安装 Git for Windows
3. 验证安装：
   ```cmd
   git --version
   ```

### 步骤 3: (可选) 安装 Inno Setup 6

如需编译安装包版 (`Setup.exe`)，需要安装 Inno Setup：

1. 访问 https://jrsoftware.org/isdl.php
2. 下载 **Inno Setup 6.x**
3. 运行安装程序
4. 默认安装路径：`C:\Program Files (x86)\Inno Setup 6\`

---

## 📂 克隆项目

### 方法 1: 从 Git 仓库克隆

```cmd
:: 创建项目目录
mkdir C:\Projects
cd C:\Projects

:: 克隆项目
git clone <repository-url> OpenClawClient

:: 进入项目目录
cd OpenClawClient
```

### 方法 2: 下载 ZIP 解压

1. 从仓库下载 ZIP 文件
2. 解压到 `C:\Projects\OpenClawClient`
3. 打开命令提示符进入该目录

---

## 🔧 本地调试

### 方法 1: 命令行运行

```cmd
cd C:\Projects\OpenClawClient

:: 还原 NuGet 包
dotnet restore

:: 运行调试
dotnet run --project src\OpenClawClient.Desktop
```

### 方法 2: Visual Studio 2022

1. 打开 `OpenClawClient.sln`
2. 右键点击解决方案 → **还原 NuGet 包**
3. 设置 `OpenClawClient.Desktop` 为启动项目
4. 按 **F5** 启动调试

### 方法 3: VS Code

1. 打开文件夹 `C:\Projects\OpenClawClient`
2. 安装 **C# Dev Kit** 扩展
3. 按 **F5** 启动调试

---

## 📦 发布绿色版

绿色版特点：
- ✅ 解压即用，无需安装
- ✅ 可放在 U 盘随身携带
- ✅ 配置文件在 `./config/` 目录

### 发布步骤

1. **打开 PowerShell**
   ```powershell
   cd C:\Projects\OpenClawClient
   ```

2. **运行发布脚本**
   ```powershell
   .\scripts\publish-portable.ps1
   ```

3. **等待构建完成**
   ```
   🚀 发布 OpenClawClient 绿色版...
   📦 编译发布...
   📦 打包 ZIP: C:\Projects\OpenClawClient\dist\OpenClawClient_Portable.zip
   ✅ 绿色版发布完成!
   ```

4. **输出文件**
   - 位置：`dist\OpenClawClient_Portable.zip`
   - 大小：约 70MB

### 使用绿色版

1. 解压 ZIP 到任意目录
2. 运行 `OpenClawClient.exe`
3. 配置文件自动创建于 `./config/appsettings.json`

---

## 📥 发布安装包版

安装包版特点：
- ✅ 标准 Windows 安装流程
- ✅ 自动创建快捷方式
- ✅ 支持控制面板卸载
- ✅ 配置文件在 `%APPDATA%`

### 前提条件

已安装 **Inno Setup 6**，且路径为：
- `C:\Program Files (x86)\Inno Setup 6\ISCC.exe` 或
- `C:\Program Files\Inno Setup 6\ISCC.exe`

### 发布步骤

1. **打开 PowerShell**
   ```powershell
   cd C:\Projects\OpenClawClient
   ```

2. **运行发布脚本**
   ```powershell
   .\scripts\publish-setup.ps1
   ```

3. **等待构建完成**
   ```
   🚀 发布 OpenClawClient 安装包版...
   📦 编译发布...
   📦 编译安装包...
   ✅ 安装包版发布完成!
   ```

4. **输出文件**
   - 位置：`dist\OpenClawClient_Setup.exe`
   - 大小：约 75MB

### 安装测试

1. 双击 `OpenClawClient_Setup.exe`
2. 按安装向导完成安装
3. 从开始菜单或桌面启动应用

---

## ✅ 验证构建

### 功能测试清单

#### 登录界面
- [ ] 输入服务器地址
- [ ] 输入 Gateway Token
- [ ] 选择文件下载路径
- [ ] 勾选"记住登录"
- [ ] 点击登录成功跳转

#### 聊天窗口
- [ ] 连接状态显示正常
- [ ] 发送文本消息成功
- [ ] 接收消息正常显示
- [ ] 消息加密图标显示 🔒

#### 文件传输
- [ ] 拖拽文件到聊天窗口
- [ ] 点击📎按钮选择文件
- [ ] Ctrl+V 粘贴图片
- [ ] 文件上传成功

#### 图片查看器
- [ ] 点击图片打开全屏查看器
- [ ] 鼠标滚轮缩放
- [ ] 拖拽平移图片
- [ ] Ctrl+S 保存图片

#### 其他功能
- [ ] 消息搜索功能
- [ ] 断网后自动重连
- [ ] 窗口位置记忆

---

## 🐛 故障排查

### 问题 1: `dotnet` 命令不存在

**原因**: .NET 8 SDK 未安装或环境变量未配置

**解决方法**:
1. 重新安装 .NET 8 SDK
2. 重启命令提示符
3. 验证：`dotnet --version`

### 问题 2: 还原 NuGet 包失败

**原因**: 网络连接问题或 NuGet 源不可用

**解决方法**:
```cmd
:: 清除 NuGet 缓存
dotnet nuget locals all --clear

:: 重试还原
dotnet restore --force
```

### 问题 3: 编译错误 "找不到类型或命名空间"

**原因**: 依赖包未正确还原

**解决方法**:
```cmd
:: 清理并重新构建
dotnet clean
dotnet restore
dotnet build
```

### 问题 4: 发布后启动报错

**可能原因**: 缺少 .NET 运行时

**解决方法**:
1. 确保发布时使用 `--self-contained true`
2. 或让用户安装 .NET 8 Runtime

### 问题 5: Inno Setup 编译失败

**错误**: "ISCC.exe 不是内部或外部命令"

**解决方法**:
1. 确认 Inno Setup 6 已安装
2. 检查安装路径是否正确
3. 手动运行 ISCC 编译：
   ```cmd
   "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\setup.iss
   ```

### 问题 6: 连接失败

**可能原因**:
1. 服务器地址错误
2. Gateway Token 无效
3. 防火墙阻止 8443 端口

**解决方法**:
1. 检查服务器地址格式：`https://www.lebacco.cn:8443`
2. 确认 Token 为 31 位十六进制
3. 检查防火墙/安全组规则

---

## 📊 构建输出对比

| 特性 | 绿色版 | 安装包版 |
|------|--------|----------|
| **文件名** | `OpenClawClient_Portable.zip` | `OpenClawClient_Setup.exe` |
| **大小** | ~70MB | ~75MB |
| **使用方式** | 解压即用 | 安装后使用 |
| **配置路径** | `./config/appsettings.json` | `%APPDATA%\OpenClawClient\` |
| **快捷方式** | 手动创建 | 自动创建 |
| **卸载** | 直接删除 | 控制面板卸载 |
| **U 盘携带** | ✅ 支持 | ❌ 不适合 |
| **多用户** | ⚠️ 共享配置 | ✅ 独立配置 |

---

## 🎯 构建时间参考

| 操作 | 时间 (参考) |
|------|-------------|
| 首次还原 NuGet 包 | 1-3 分钟 |
| 清理后重新编译 | 30-60 秒 |
| 发布绿色版 | 1-2 分钟 |
| 发布安装包版 | 2-3 分钟 |

---

## 📝 附录：PowerShell 执行策略

如果运行脚本时遇到权限错误：

```powershell
# 查看当前策略
Get-ExecutionPolicy

# 临时允许当前会话
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass

# 或永久允许（需要管理员权限）
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```

---

## 📚 相关文档

- [开发指南.md](./开发指南.md) - 详细开发说明
- [项目总结.md](./项目总结.md) - 功能清单和架构
- [README.md](../README.md) - 项目概述

---

**最后更新**: 2026-03-24  
**适用版本**: v1.0.0
