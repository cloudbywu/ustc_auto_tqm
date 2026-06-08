# AutoTQM — USTC 教学质量评估自动化工具

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![Playwright](https://img.shields.io/badge/Playwright-1.41-green)](https://playwright.dev/)
[![License](https://img.shields.io/badge/license-MIT-yellow)](LICENSE)

> 基于 .NET 8 + Playwright 重构的 USTC 教学质量评估（TQM）自动评教工具。
> 跨平台（Windows / macOS / Linux），无需外部浏览器驱动，模块化设计，支持调试与截图。

---

## ✨ 功能特性

- **🖥️ 跨平台支持** — Windows、macOS、Linux 原生运行
- **🔧 零外部驱动依赖** — 使用 Playwright 自动管理 Chromium，无需手动下载 `msedgedriver.exe`
- **🧩 模块化架构** — 浏览器 / 认证 / 评教三层解耦，接口隔离，易于扩展与测试
- **⏱️ 智能二次验证检测** — 轮询检测登录状态，验证完成自动继续，无需傻等
- **👨‍🏫 多教师/助教支持** — 自动识别"下一位教师"与"下一门课程"，正确处理含助教的课程
- **🐛 调试友好** — 支持 `--debug` 日志、`--screenshot` 截图、`--slowmo` 慢动作模式
- **⚙️ 灵活配置** — `appsettings.json` + 命令行参数 + 环境变量三重配置

---

## 📦 技术栈

| 组件 | 版本 | 说明 |
|------|------|------|
| .NET | 8.0 | 运行时与 SDK |
| Microsoft.Playwright | 1.41.2 | 跨平台浏览器自动化 |
| C# 12 | — | 主开发语言 |

---

## 🚀 快速开始

### 1. 克隆仓库

```bash
git clone https://github.com/yourusername/AutoTQM.git
cd AutoTQM/AutoTqm
```

### 2. 安装依赖

```bash
# 还原 NuGet 包
dotnet restore

# 安装 Playwright 浏览器（仅首次）
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium
```

> 如果 `playwright install` 提示版本不匹配，改用：
> ```bash
> dotnet tool install Microsoft.Playwright.CLI --local
> dotnet playwright install chromium
> ```

### 3. 配置凭据

复制配置模板并填写你的学号密码：

```bash
cp AutoTqm.Cli/appsettings.example.json AutoTqm.Cli/appsettings.json
```

编辑 `AutoTqm.Cli/appsettings.json`：

```json
{
  "StudentId": "PB23XXXXXX",
  "Password": "your_password",
  "TwoFactorWaitSeconds": 60,
  "RadioValue": "1",
  "CheckboxValue": "1",
  "DelayMs": 2000,
  "SubmitWaitMs": 6000,
  "SlowMo": 0,
  "ScreenshotDir": "screenshots"
}
```

> ⚠️ **安全提示**：`appsettings.json` 已加入 `.gitignore`，不会被提交到 GitHub。

### 4. 编译运行

```bash
dotnet build
dotnet run --project AutoTqm.Cli
```

---

## 🎮 CLI 参数

| 参数 | 说明 |
|------|------|
| `--debug` | 开启 Debug 级日志，输出单选/多选匹配详情 |
| `--headless` | 无头模式（不显示浏览器窗口） |
| `--slowmo=500` | 慢动作模式，每步延迟 500ms，方便观察 |
| `--screenshot` | 评价过程中自动截图保存到 `screenshots/` |
| `--single` | 仅评价第一门课程（调试用） |

### 常用组合

```bash
# 调试模式：可视化 + 截图 + 慢动作
dotnet run --project AutoTqm.Cli -- --debug --screenshot --slowmo=300

# 无头模式（后台静默运行）
dotnet run --project AutoTqm.Cli -- --headless

# 单课程调试
dotnet run --project AutoTqm.Cli -- --single --debug --screenshot
```

---

## ⚙️ 配置说明

配置优先级：**命令行参数 > 环境变量 > `appsettings.json`**

| 配置项 | 默认值 | 说明 |
|--------|--------|------|
| `StudentId` | `""` | 学号 |
| `Password` | `""` | 密码 |
| `TwoFactorWaitSeconds` | `60` | 二次验证最大等待时间（秒） |
| `RadioValue` | `"1"` | 单选按钮选中值（1=最好评） |
| `CheckboxValue` | `"1"` | 多选框选中值 |
| `DelayMs` | `2000` | 操作间隔延迟（毫秒） |
| `SubmitWaitMs` | `6000` | 提交后等待时间（毫秒） |
| `SlowMo` | `0` | Playwright 慢动作延迟（毫秒） |
| `ScreenshotDir` | `"screenshots"` | 截图保存目录 |

---

## 🐛 调试技巧

1. **可视化调试**：去掉 `--headless`，加 `--slowmo=300`，实时观察每一步
2. **截图追踪**：加 `--screenshot`，每份表单评价前后自动截图
3. **日志分级**：加 `--debug` 查看元素匹配详情与按钮点击记录
4. **单课程模式**：加 `--single` 只跑一门课，快速验证 XPath/选择器

---

## 📁 项目结构

```
AutoTqm/
├── AutoTqm.sln                          # 解决方案
├── AutoTqm.Core/                        # 核心库（可复用、可测试）
│   ├── Interfaces/
│   │   ├── IBrowserService.cs           # 浏览器抽象
│   │   ├── IAuthenticationService.cs    # 认证抽象
│   │   ├── IEvaluationService.cs        # 评教抽象
│   │   ├── IConfigurationService.cs     # 配置抽象
│   │   └── ILogger.cs                   # 日志抽象
│   ├── Models/
│   │   ├── UserCredentials.cs           # 用户凭据
│   │   ├── CourseInfo.cs                # 课程信息
│   │   └── EvaluationOptions.cs         # 评价选项
│   └── Services/
│       ├── PlaywrightBrowserService.cs    # Playwright 浏览器实现
│       ├── TqmAuthenticationService.cs    # 登录流程实现
│       ├── TqmEvaluationService.cs        # 评教流程实现
│       ├── ConsoleLogger.cs             # 控制台日志
│       └── JsonConfigurationService.cs  # JSON 配置
└── AutoTqm.Cli/                         # 命令行入口
    ├── Program.cs
    └── appsettings.json                 # 本地配置文件（已 gitignore）
```

---

## 🏗️ 发布单文件可执行程序

```bash
# Windows
dotnet publish AutoTqm.Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# macOS
dotnet publish AutoTqm.Cli -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true

# Linux
dotnet publish AutoTqm.Cli -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

---

## ⚠️ 注意事项

1. **请勿将 `appsettings.json` 提交到 GitHub**，其中包含明文密码。仓库已配置 `.gitignore` 自动排除。
2. **二次验证**：首次登录可能需要短信/扫码验证，程序会自动轮询检测，完成后自动继续。
3. **评教频率**：请遵守学校相关规定，合理使用自动化工具。
4. **截图文件**：运行产生的 `screenshots/` 目录不会被提交，可定期清理。

---

## 📄 许可证

[MIT License](LICENSE)

---

> 本项目仅供学习交流使用，作者不对因使用本工具产生的任何后果负责。
