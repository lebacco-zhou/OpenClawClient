# GitHub 推送命令
# 用法：在 OpenClawClient 目录中执行

# 替换为你的 GitHub 仓库地址
YOUR_GITHUB_REPO="https://github.com/你的用户名/OpenClawClient.git"

# 添加远程仓库
git remote add origin $YOUR_GITHUB_REPO

# 验证远程仓库
git remote -v

# 推送所有分支和标签
git push -u origin master

# 如果有多个分支，推送所有分支
# git push --all origin

# 推送所有标签
# git push --tags origin

echo "✅ 推送完成！"
echo "访问 GitHub 仓库查看代码：$YOUR_GITHUB_REPO"
