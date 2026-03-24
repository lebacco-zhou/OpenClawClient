# GitHub 推送认证指南

## 🔐 问题说明

执行 `git push` 时需要 GitHub 认证。由于 GitHub 已禁用密码推送，需要使用 **Personal Access Token (PAT)**。

---

## ✅ 方案 1: 使用 Personal Access Token（推荐）

### 步骤 1: 创建 Personal Access Token

1. **访问 GitHub Token 页面**
   - 登录 GitHub
   - 访问：https://github.com/settings/tokens

2. **点击 "Generate new token (classic)"**

3. **填写信息**
   - **Note**: `OpenClawClient Push Token`
   - **Expiration**: 选择 `90 days` 或 `No expiration`
   - **Scopes**（权限）：勾选以下项
     - ✅ `repo` (Full control of private repositories)
     - ✅ `workflow` (Update GitHub Action workflows)
     - ✅ `write:packages` (Upload packages)

4. **点击 "Generate token"**

5. **复制 Token**
   - ⚠️ **重要**：Token 只显示一次！立即复制到安全地方
   - 格式：`ghp_xxxxxxxxxxxxxxxxxxxx`

---

### 步骤 2: 使用 Token 推送代码

**方式 A: 直接在命令中使用 Token**

```bash
cd /root/.openclaw/workspace/OpenClawClient

# 使用 Token 推送（替换 YOUR_TOKEN）
git push https://lebacco-zhou:YOUR_TOKEN@github.com/lebacco-zhou/OpenClawClient.git master
```

**方式 B: 配置 Git 记住凭证**

```bash
# 配置 Git 记住凭证
git config --global credential.helper store

# 然后正常推送（会提示输入用户名和 Token）
git push -u origin master

# 输入：
# Username: lebacco-zhou
# Password: 粘贴你的 Token（不是 GitHub 密码！）
```

---

## ✅ 方案 2: 使用 SSH 密钥（一劳永逸）

### 步骤 1: 生成 SSH 密钥

```bash
# 生成 SSH 密钥（按提示操作，可一直回车）
ssh-keygen -t ed25519 -C "your_email@example.com"

# 查看公钥
cat ~/.ssh/id_ed25519.pub
```

### 步骤 2: 添加 SSH 公钥到 GitHub

1. 复制 `cat ~/.ssh/id_ed25519.pub` 的输出
2. 访问：https://github.com/settings/keys
3. 点击 "New SSH key"
4. 粘贴公钥，保存

### 步骤 3: 改用 SSH 地址推送

```bash
# 删除 HTTPS 远程仓库
git remote remove origin

# 添加 SSH 远程仓库
git remote add origin git@github.com:lebacco-zhou/OpenClawClient.git

# 推送
git push -u origin master
```

---

## 🚀 快速推送命令（使用 Token）

```bash
cd /root/.openclaw/workspace/OpenClawClient

# 方式 1: 临时使用 Token（推荐首次使用）
git push https://lebacco-zhou:ghp_xxxxxxxxxxxxxxxxxxxx@github.com/lebacco-zhou/OpenClawClient.git master

# 方式 2: 配置凭证后推送
git config --global credential.helper store
git push -u origin master
# 输入用户名：lebacco-zhou
# 输入密码：粘贴 Token
```

---

## ⚠️ 安全提示

1. **Token 保密**：不要提交到代码库或公开分享
2. **Token 过期**：建议设置 90 天过期，定期更新
3. **最小权限**：只授予必要的权限
4. **泄露处理**：如 Token 泄露，立即在 GitHub 删除并重新生成

---

## 📋 完整推送流程

```bash
# 1. 进入项目目录
cd /root/.openclaw/workspace/OpenClawClient

# 2. 确认远程仓库
git remote -v
# 应显示：origin  https://github.com/lebacco-zhou/OpenClawClient.git

# 3. 推送代码（使用 Token）
git push https://lebacco-zhou:YOUR_TOKEN@github.com/lebacco-zhou/OpenClawClient.git master

# 4. 验证推送
git status
# 应显示：Your branch is up to date with 'origin/master'.
```

---

## 🎯 下一步

推送成功后：

1. **访问 GitHub 仓库**
   - https://github.com/lebacco-zhou/OpenClawClient

2. **确认代码已上传**
   - 查看文件列表
   - 查看提交历史（应有 7 次提交）

3. **在 Windows 上克隆**
   ```cmd
   git clone https://github.com/lebacco-zhou/OpenClawClient.git
   cd OpenClawClient
   dotnet restore
   dotnet run --project src\OpenClawClient.Desktop
   ```

---

## ❓ 需要帮助？

如果遇到问题，告诉我错误信息，我会帮你解决！

---

**最后更新**: 2026-03-24  
**适用版本**: v1.0.0
