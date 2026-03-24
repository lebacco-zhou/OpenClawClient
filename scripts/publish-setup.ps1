# 安装包版发布脚本
# 用法：.\scripts\publish-setup.ps1
# 需要：Inno Setup 6

$ErrorActionPreference = "Stop"

Write-Host "🚀 发布 OpenClawClient 安装包版..." -ForegroundColor Green

# 设置路径
$projectRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $projectRoot "publish\setup"
$distDir = Join-Path $projectRoot "dist"
$installerScript = Join-Path $projectRoot "installer\setup.iss"

# 清理旧文件
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}
if (Test-Path $distDir) {
    New-Item $distDir -ItemType Directory -Force | Out-Null
}

# 发布应用
Write-Host "📦 编译发布..." -ForegroundColor Yellow
dotnet publish "$projectRoot\src\OpenClawClient.Desktop\OpenClawClient.Desktop.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:EnableCompressionInSingleFile=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 发布失败!" -ForegroundColor Red
    exit 1
}

# 创建配置文件模板目录
$configDir = "$env:APPDATA\OpenClawClient"
if (-not (Test-Path $configDir)) {
    New-Item -ItemType Directory -Force -Path $configDir | Out-Null
}

# 编译 Inno Setup 安装包
Write-Host "📦 编译安装包..." -ForegroundColor Yellow

# 查找 Inno Setup 安装路径
$isccPaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
)

$isccExe = $null
foreach ($path in $isccPaths) {
    if (Test-Path $path) {
        $isccExe = $path
        break
    }
}

if ($null -eq $isccExe) {
    Write-Host "⚠️  未找到 Inno Setup，跳过安装包编译" -ForegroundColor Yellow
    Write-Host "💡 请安装 Inno Setup 6: https://jrsoftware.org/isdl.php" -ForegroundColor Cyan
} else {
    & $isccExe $installerScript /O"$distDir"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ 安装包编译失败!" -ForegroundColor Red
        exit 1
    }
}

Write-Host "✅ 安装包版发布完成!" -ForegroundColor Green
Write-Host "📁 输出目录：$distDir" -ForegroundColor Cyan
