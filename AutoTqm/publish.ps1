# 一键发布脚本：编译 + 打包 Chromium 浏览器（离线可用）
# 用法:
#   .\publish.ps1              # 默认发布当前平台 (Windows)
#   .\publish.ps1 -Rid linux-x64   # 发布 Linux 版本
#   .\publish.ps1 -Rid osx-x64     # 发布 macOS 版本

param(
    [string]$Rid = "win-x64",
    [string]$Config = "Release"
)

$ErrorActionPreference = "Stop"

$project = "AutoTqm.Cli"
$publishDir = "$PSScriptRoot\$project\bin\$Config\net8.0\$Rid\publish"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  AutoTQM 一键发布（含 Chromium）" -ForegroundColor Cyan
Write-Host "  目标平台: $Rid" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1. 编译并发布
Write-Host "`n[1/3] 正在发布 .NET 程序 ($Rid) …" -ForegroundColor Yellow
dotnet publish $project -c $Config -r $Rid --self-contained true
if ($LASTEXITCODE -ne 0) { throw "发布失败" }

# 2. 安装 Chromium 到发布目录
Write-Host "`n[2/3] 正在安装 Chromium 到发布目录 …" -ForegroundColor Yellow
$env:PLAYWRIGHT_BROWSERS_PATH = "$publishDir\.playwright"
$playwrightPs1 = "$publishDir\playwright.ps1"

if (Test-Path $playwrightPs1) {
    # 兼容 Windows PowerShell 5.x 和 PowerShell 7+
    & $playwrightPs1 install chromium
} else {
    # 回退：尝试用全局 playwright CLI
    playwright install chromium
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n[警告] Chromium 自动安装失败。如果目标平台与当前系统不同（如交叉编译 Linux），" -ForegroundColor Yellow
    Write-Host "       这是正常的。请在目标 Linux/macOS 机器上手动运行：" -ForegroundColor Yellow
    Write-Host "       export PLAYWRIGHT_BROWSERS_PATH=./.playwright" -ForegroundColor Cyan
    Write-Host "       ./playwright.sh install chromium" -ForegroundColor Cyan
}

# 3. 复制配置文件模板
Write-Host "`n[3/3] 复制配置文件模板 …" -ForegroundColor Yellow
$exampleConfig = "$PSScriptRoot\$project\appsettings.example.json"
$targetConfig = "$publishDir\appsettings.json"
if (Test-Path $exampleConfig) {
    Copy-Item $exampleConfig $targetConfig -Force
    Write-Host "已生成 $targetConfig，请编辑填写学号密码" -ForegroundColor Green
}

# 4. 输出结果
Write-Host "`n========================================" -ForegroundColor Green
Write-Host "  发布完成！" -ForegroundColor Green
Write-Host "  目标平台: $Rid" -ForegroundColor Green
Write-Host "  输出目录: $publishDir" -ForegroundColor Green
Write-Host "  总大小: " -NoNewline -ForegroundColor Green
$size = (Get-ChildItem $publishDir -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "$([math]::Round($size, 1)) MB" -ForegroundColor Green

if ($Rid -eq "win-x64") {
    Write-Host "`n  运行方式: .\AutoTqm.Cli.exe" -ForegroundColor Cyan
} elseif ($Rid -eq "linux-x64") {
    Write-Host "`n  运行方式: chmod +x AutoTqm.Cli && ./AutoTqm.Cli" -ForegroundColor Cyan
} elseif ($Rid -eq "osx-x64") {
    Write-Host "`n  运行方式: chmod +x AutoTqm.Cli && ./AutoTqm.Cli" -ForegroundColor Cyan
}

Write-Host "`n  分发时请将整个 publish 文件夹一起复制！" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Green
