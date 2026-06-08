#!/bin/bash
# 一键发布脚本（Linux / macOS 版）
# 用法:
#   ./publish.sh              # 默认发布当前平台
#   ./publish.sh linux-x64    # 显式指定平台
#   ./publish.sh osx-x64

set -e

RID="${1:-$(dotnet --info | grep 'RID:' | awk '{print $2}')}"
CONFIG="Release"
PROJECT="AutoTqm.Cli"
PUBLISH_DIR="$PROJECT/bin/$CONFIG/net8.0/$RID/publish"

echo "========================================"
echo "  AutoTQM 一键发布（含 Chromium）"
echo "  目标平台: $RID"
echo "========================================"

# 1. 编译并发布
echo ""
echo "[1/3] 正在发布 .NET 程序 ($RID) …"
dotnet publish "$PROJECT" -c "$CONFIG" -r "$RID" --self-contained true

# 2. 安装 Chromium 到发布目录
echo ""
echo "[2/3] 正在安装 Chromium 到发布目录 …"
export PLAYWRIGHT_BROWSERS_PATH="$PUBLISH_DIR/.playwright"

if [ -f "$PUBLISH_DIR/playwright.sh" ]; then
    bash "$PUBLISH_DIR/playwright.sh" install chromium
elif command -v playwright &> /dev/null; then
    playwright install chromium
else
    echo ""
    echo "[警告] 未找到 playwright 安装脚本。"
    echo "       请在目标机器上手动安装浏览器："
    echo "       export PLAYWRIGHT_BROWSERS_PATH=./.playwright"
    echo "       ./playwright.sh install chromium"
fi

# 3. 复制配置文件模板
echo ""
echo "[3/3] 复制配置文件模板 …"
if [ -f "$PROJECT/appsettings.example.json" ]; then
    cp "$PROJECT/appsettings.example.json" "$PUBLISH_DIR/appsettings.json"
    echo "已生成 $PUBLISH_DIR/appsettings.json，请编辑填写学号密码"
fi

# 4. 输出结果
echo ""
echo "========================================"
echo "  发布完成！"
echo "  目标平台: $RID"
echo "  输出目录: $PUBLISH_DIR"
echo "  总大小: $(du -sh "$PUBLISH_DIR" 2>/dev/null | cut -f1)"
echo ""

if [ "$RID" = "win-x64" ]; then
    echo "  运行方式: .\\AutoTqm.Cli.exe"
else
    echo "  运行方式: chmod +x $PUBLISH_DIR/AutoTqm.Cli && $PUBLISH_DIR/AutoTqm.Cli"
fi

echo ""
echo "  分发时请将整个 publish 文件夹一起复制！"
echo "========================================"
