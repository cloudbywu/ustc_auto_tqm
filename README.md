# AutoTQM

USTC 教学质量评估（TQM）自动评教工具 — .NET 8 + Playwright 重构版

## 快速开始

```bash
cd AutoTqm
dotnet restore
dotnet build
cp AutoTqm.Cli/appsettings.example.json AutoTqm.Cli/appsettings.json
# 编辑 appsettings.json 填写学号密码
dotnet run --project AutoTqm.Cli
```

详见 [README.md](AutoTqm/README.md)
