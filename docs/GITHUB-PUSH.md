# OpenClaw Client - GitHub 推送指南

## 📦 快速推送（推荐）

### 1. 在 GitHub 创建仓库

1. 访问 https://github.com/new
2. 仓库名：`OpenClawClient`
3. 描述：`安全加密的 Windows 桌面聊天客户端`
4. 设为 **Private**（推荐）或 **Public**
5. ❌ 不要初始化 README、.gitignore、License
6. 点击 **Create repository**

### 2. 获取仓库地址

创建后复制显示的地址，格式：
```
https://github.com/你的用户名/OpenClawClient.git
```

### 3. 执行推送命令

```bash
cd /root/.openclaw/workspace/OpenClawClient

# 添加远程仓库（替换为你的地址）
git remote add origin https://github.com/你的用户名/OpenClawClient.git

# 推送代码
git push -u origin master
```

### 4. 验证推送

访问 GitHub 仓库页面，确认代码已上传：
```
https://github.com/你的用户名/OpenClawClient
```

---

## 🔐 使用 SSH 方式（可选）

如果配置了 SSH 密钥，可以使用 SSH 地址：

```bash
# SSH 地址格式
git@github.com:你的用户名/OpenClawClient.git

# 添加远程仓库
git remote add origin git@github.com:你的用户名/OpenClawClient.git

# 推送
git push -u origin master
```

---

## 📋 完整命令参考

```bash
# 进入项目目录
cd /root/.openclaw/workspace/OpenClawClient

# 查看当前状态
git status

# 查看提交历史
git log --oneline

# 添加远程仓库
git remote add origin https://github.com/你的用户名/OpenClawClient.git

# 验证远程仓库
git remote -v

# 推送代码
git push -u origin master

# 查看推送状态
git status
```

---

## 🔄 后续更新代码

首次推送后，后续更新：

```bash
# 修改代码后
git add .
git commit -m "描述你的修改"
git push
```

---

## 📥 克隆仓库（在其他设备）

```bash
# 克隆到 Windows
git clone https://github.com/你的用户名/OpenClawClient.git
cd OpenClawClient

# 还原依赖
dotnet restore

# 运行
dotnet run --project src\OpenClawClient.Desktop
```

---

## ⚠️ 常见问题

### Q: `git remote add origin` 报错 "remote origin already exists"
**A**: 远程仓库已存在，先删除再添加：
```bash
git remote remove origin
git remote add origin <新地址>
```

### Q: `git push` 报错 "Permission denied"
**A**: 
1. 检查 GitHub 用户名是否正确
2. 确认有仓库写入权限
3. 或使用 SSH 方式（需配置 SSH 密钥）

### Q: 推送大文件失败
**A**: GitHub 单文件限制 100MB，使用 Git LFS：
```bash
git lfs install
git lfs track "*.zip"
git add .gitattributes
git commit -m "Configure LFS"
git push
```

---

**最后更新**: 2026-03-24  
**适用版本**: v1.0.0
