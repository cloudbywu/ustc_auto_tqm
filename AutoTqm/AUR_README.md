# AUR 打包指南

## 文件说明

| 文件 | 用途 |
|------|------|
| `PKGBUILD` | Arch 构建脚本，定义依赖、编译、打包流程 |
| `autotqm.install` | 安装/升级后的提示信息脚本 |

## 本地构建测试

```bash
cd AutoTqm

# 生成 .SRCINFO（提交 AUR 必需）
makepkg --printsrcinfo > .SRCINFO

# 构建并安装包
makepkg -si

# 仅构建不安装
makepkg
```

## 提交到 AUR

```bash
# 1. 安装 aurutils 或手动操作
sudo pacman -S aurutils

# 2. 初始化 AUR 仓库（首次）
mkdir -p ~/aur && cd ~/aur
git clone ssh://aur@aur.archlinux.org/autotqm.git

# 3. 复制构建文件
cp /path/to/ustc_auto_tqm/AutoTqm/{PKGBUILD,.SRCINFO,autotqm.install} ~/aur/autotqm/

# 4. 提交
cd ~/aur/autotqm
git add .
git commit -m "Update to v1.0.0"
git push origin master
```

## 包行为

- **程序文件**：安装到 `/opt/autotqm/`（包含自包含 .NET 运行时 + Chromium 浏览器）
- **启动命令**：`/usr/bin/autotqm`
- **配置文件**：首次运行时自动复制模板到 `~/.config/autotqm/appsettings.json`
- **离线运行**：Chromium 浏览器已打包，无需联网下载

## 依赖说明

| 类型 | 包名 | 说明 |
|------|------|------|
| 构建依赖 | `dotnet-sdk>=8.0` | 编译 .NET 程序 |
| 运行依赖 | `nss` `nspr` `atk` … | Chromium 所需的系统库 |

> 自包含 .NET 二进制已包含运行时，因此不需要 `dotnet-runtime`。

## 常见问题

**Q: 构建时下载 Chromium 很慢？**
A: 这是正常的，Chromium 约 150-200 MB。可以使用代理或等待。

**Q: 如何更新到最新 Git 版本？**
A: `makepkg -si` 会自动拉取最新源码重新构建。

**Q: 配置文件在哪？**
A: `~/.config/autotqm/appsettings.json`，首次运行 `autotqm` 会自动创建。
