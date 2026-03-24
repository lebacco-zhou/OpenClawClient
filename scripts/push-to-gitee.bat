@echo off
chcp 65001 >nul
echo ====================================
echo Gitee 推送脚本 - OpenClaw Client
echo ====================================
echo.

cd /d D:\Projects\OpenClawClient

echo [1] 检查 Git 配置...
git --version
echo.

echo [2] 配置远程仓库...
git remote remove origin 2>nul
git remote add origin https://gitee.com/lebacco/open-claw-client.git
git remote -v
echo.

echo [3] 推送到 Gitee...
echo 使用私人令牌推送中...
git push https://lebacco-zhou:230aa235202d62533719f8f909087278@gitee.com/lebacco/open-claw-client.git master

if %errorlevel% equ 0 (
    echo.
    echo ====================================
    echo ✅ 推送成功！
    echo ====================================
    echo.
    echo 访问仓库：https://gitee.com/lebacco/open-claw-client
) else (
    echo.
    echo ====================================
    echo ❌ 推送失败，错误代码：%errorlevel%
    echo ====================================
    echo.
    echo 请检查：
    echo 1. 网络连接是否正常
    echo 2. 私人令牌是否正确
    echo 3. Gitee 仓库是否存在
)

echo.
pause
