# 一键发布脚本：编译 + 打包 Chromium 浏览器（离线可用）
# 用法: .\publish.ps1

$ErrorActionPreference = "Stop"

$project = "AutoTqm.Cli"
$config = "Release"
$rid = "win-x64"
$publishDir = "$PSScriptRoot\$project\bin\$config\net8.0\$rid\publish"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  AutoTQM 一键发布（含 Chromium）" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1. 编译并发布
Write-Host "`n[1/3] 正在发布 .NET 程序 …" -ForegroundColor Yellow
dotnet publish $project -c $config -r $rid --self-contained true
if ($LASTEXITCODE -ne 0) { throw "发布失败" }

# 2. 安装 Chromium 到发布目录
Write-Host "`n[2/3] 正在安装 Chromium 到发布目录 …" -ForegroundColor Yellow
$env:PLAYWRIGHT_BROWSERS_PATH = "$publishDir\.playwright"
$playwrightPs1 = "$publishDir\playwright.ps1"

if (Test-Path $playwrightPs1) {
    pwsh $playwrightPs1 install chromium
} else {
    # 回退：尝试用全局 playwright CLI
    playwright install chromium
}

if ($LASTEXITCODE -ne 0) { throw "Chromium 安装失败" }

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
Write-Host "  输出目录: $publishDir" -ForegroundColor Green
Write-Host "  总大小: " -NoNewline -ForegroundColor Green
$size = (Get-ChildItem $publishDir -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "$([math]::Round($size, 1)) MB" -ForegroundColor Green
Write-Host "`n  分发时请将整个 publish 文件夹一起复制！" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Green
