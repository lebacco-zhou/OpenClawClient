# 绿色版发布脚本
# 用法：.\scripts\publish-portable.ps1

$ErrorActionPreference = "Stop"

Write-Host "🚀 发布 OpenClawClient 绿色版..." -ForegroundColor Green

# 设置路径
$projectRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $projectRoot "publish\portable"
$distDir = Join-Path $projectRoot "dist"

# 清理旧文件
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}
if (Test-Path $distDir) {
    New-Item $distDir -ItemType Directory -Force | Out-Null
}

# 发布单文件版本
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

# 创建配置目录
$configDir = Join-Path $publishDir "config"
New-Item -ItemType Directory -Force -Path $configDir | Out-Null

# 创建配置文件模板
$configTemplate = @{
    serverUrl = "https://www.lebacco.cn:8443"
    gatewayToken = ""
    aesKey = ""
    downloadPath = ".\Downloads"
    autoSubfolder = $true
    rememberLogin = $true
} | ConvertTo-Json -Depth 10

$configTemplate | Out-File -FilePath (Join-Path $configDir "appsettings.template.json") -Encoding UTF8

# 打包 ZIP
$zipPath = Join-Path $distDir "OpenClawClient_Portable.zip"
Write-Host "📦 打包 ZIP: $zipPath" -ForegroundColor Yellow

Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath -Force

Write-Host "✅ 绿色版发布完成!" -ForegroundColor Green
Write-Host "📁 输出文件：$zipPath" -ForegroundColor Cyan
