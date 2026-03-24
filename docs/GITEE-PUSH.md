# 🐹 Gitee 推送指南

## 📦 快速推送步骤

### 步骤 1: 在 Gitee 创建仓库

1. **访问** https://gitee.com/new
2. **填写信息**：
   - **仓库名称**: `OpenClawClient`
   - **仓库介绍**: `安全加密的 Windows 桌面聊天客户端 - .NET 8 WPF`
   - **开源/私有**: 选择 **私有**（推荐）
   - ❌ 不勾选 "初始化 README"
   - ❌ 不勾选 ".gitignore"
   - ❌ 不勾选 "开源许可证"
3. **点击 "创建"**

---

### 步骤 2: 获取 Gitee 仓库地址

创建后复制地址：
```
https://gitee.com/lebacco-zhou/OpenClawClient.git
```

---

### 步骤 3: 配置 Git 远程仓库

```bash
cd /root/.openclaw/workspace/OpenClawClient

# 删除旧的 GitHub 远程仓库
git remote remove origin

# 添加 Gitee 远程仓库
git remote add origin https://gitee.com/lebacco-zhou/OpenClawClient.git

# 验证
git remote -v
```

---

### 步骤 4: 推送代码到 Gitee

**方式 A: 使用 HTTPS + 密码（推荐）**

```bash
cd /root/.openclaw/workspace/OpenClawClient

# 推送代码（会提示输入 Gitee 用户名和密码）
git push -u origin master

# 输入：
# Username: lebacco-zhou
# Password: 你的 Gitee 密码
```

**方式 B: 使用 SSH（一劳永逸）**

```bash
# 1. 生成 SSH 密钥
ssh-keygen -t ed25519 -C "your_email@example.com"

# 2. 查看公钥
cat ~/.ssh/id_ed25519.pub

# 3. 复制公钥，访问：https://gitee.com/profile/ssh_keys
# 4. 添加 SSH 公钥

# 5. 改用 SSH 地址
git remote set-url origin git@gitee.com:lebacco-zhou/OpenClawClient.git

# 6. 推送
git push -u origin master
```

---

## 🔐 Gitee 开启双重认证

如果开启了双重认证，需要使用 **私人令牌**：

### 生成私人令牌

1. 访问：https://gitee.com/profile/personal_access_tokens
2. 点击 "生成新令牌"
3. 权限勾选：
   - ✅ `projects` (项目)
   - ✅ `pull_requests` (合并请求)
4. 点击 "提交"
5. **复制令牌**（只显示一次！）

### 使用私人令牌推送

```bash
git push https://lebacco-zhou:私人令牌@gitee.com/lebacco-zhou/OpenClawClient.git master
```

---

## 📊 Gitee vs GitHub

| 特性 | Gitee | GitHub |
|------|-------|--------|
| **国内访问** | ✅ 快速 | ⚠️ 可能慢 |
| **中文界面** | ✅ 支持 | ⚠️ 英文 |
| **私有仓库** | ✅ 免费 | ✅ 免费 |
| **文件大小限制** | 100MB | 100MB |
| **SSH 支持** | ✅ | ✅ |
| **双重认证** | ✅ | ✅ |

---

## 🚀 在 Windows 上克隆

```cmd
:: 创建项目目录
mkdir D:\Projects
cd D:\Projects

:: 克隆 Gitee 仓库
git clone https://gitee.com/lebacco-zhou/OpenClawClient.git

:: 进入项目
cd OpenClawClient

:: 还原依赖
dotnet restore

:: 运行
dotnet run --project src\OpenClawClient.Desktop
```

---

## ⚠️ 常见问题

### Q: `git push` 报错 "Permission denied"
**A**: 检查：
1. Gitee 用户名是否正确
2. 是否有仓库写入权限
3. 如开启双重认证，使用私人令牌

### Q: HTTPS 推送太慢
**A**: 使用 SSH 方式：
```bash
git remote set-url origin git@gitee.com:lebacco-zhou/OpenClawClient.git
```

### Q: 推送大文件失败
**A**: 使用 Git LFS：
```bash
git lfs install
git lfs track "*.zip"
git add .gitattributes
git commit -m "Configure LFS"
git push
```

---

## 📚 相关文档

- [Gitee 官方文档](https://gitee.com/help)
- [Git 配置代理](https://gitee.com/help/articles/424)
- [SSH 公钥配置](https://gitee.com/help/articles/418)

---

**最后更新**: 2026-03-24  
**适用版本**: v1.0.0
