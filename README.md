<h1 align="center">🎨 Snet.WpfMUI</h1>

<p align="center">
  <img width="120" height="120" src="https://api.snet.cn/pic/nuget.png" alt="Snet Logo"/>
</p>

<p align="center">
  <b>基于 WPF 的现代化界面库 — 50+ 常用控件与界面基础架构，开箱即用</b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-blue?logo=dotnet"/>
  <img src="https://img.shields.io/badge/.NET-10.0-blue?logo=dotnet"/>
  <img src="https://img.shields.io/badge/license-MIT-green"/>
  <img src="https://img.shields.io/nuget/v/Snet.Windows.Controls?color=blue"/>
  <img src="https://img.shields.io/github/stars/shunnet/WpfMUI?style=social"/>
</p>

<p align="center">
  <a href="https://snet.cn"><b>🌐 官方网站</b></a> ·
  <a href="https://github.com/shunnet/WpfMUI"><b>📦 GitHub</b></a> ·
  <a href="https://snet.cn/7EUf6"><b>🎬 演示视频</b></a>
</p>

<p align="center">
  📖 <a href="README.en.md"><b>English</b></a> | 简体中文
</p>

## ✨ 特性亮点

| 特性 | 说明 |
|------|------|
| 🔗 **MVVM 支持** | 视图与视图模型绑定注入（BindNotify / EventCommand / RoutedEventTrigger） |
| 🧩 **50+ 控件** | 基础控件、数据展示、文本编辑、对话框、系统集成一应俱全 |
| 🌐 **多语言引擎** | 完整的 ResX 本地化支持（LocalizeDictionary / LocExtension 等） |
| 🌓 **主题切换** | 暗色 / 亮色主题，图表与控件跟随变色 |
| 🪟 **窗口基类** | WindowBase 封装主题、图标、皮肤、语言处理 |
| 🔧 **高扩展性** | 便于二次开发与新增控件 |

## 🧩 控件库 — Snet.Windows.Controls

| 分类 | 控件 |
|------|------|
| 🎯 **基础控件** | ButtonControl · ComboBoxControl · TextBoxControl · CheckMark · RadioButtonList · SpinControl · SliderEx 等 |
| 📊 **数据展示** | DataGrid · TreeListBox · PropertyControl / PropertyGrid · LedGaugeControl · PageBarControl · ColorPicker · HeaderedEntrySlider |
| 📝 **文本编辑** | TextEditor（完整 AvalonEdit 移植：语法高亮 / 代码折叠 / 代码补全 / 搜索 / Snippet）· FormattingTextBox · EditableTextBlock |
| 💬 **对话框** | MessageBox（OK / OKCancel / Yes / YesNo 四种）· PropertyDialog · WizardDialog · BrowseForFolderDialog |
| 🚀 **系统集成** | NotifyIcon（系统托盘 + TrayManager）· DragControls（拖拽动画）· DockPanelSplitter |
| 🧩 **扩展与工具** | TextBlockEx · TextBoxEx · LinkBlock · EnumMenuItem · ButtonChrome · FilePicker / DirectoryPicker · DataAnnotations（60+ 验证特性）· Converters（26+ 值转换器） |

## 🏗️ 核心库 — Snet.Windows.Core

| 模块 | 说明 |
|------|------|
| 🪟 **WindowBase** | 窗口基类，封装主题 / 图标 / 皮肤 / 语言处理 |
| 🧩 **MVVM 辅助** | BindNotify（通知基类）、EventCommand、RoutedEventTrigger |
| 🌐 **多语言引擎** | LocalizeDictionary、LocExtension / BLoc / FELoc、ResxLocalizationProvider |
| 🌓 **主题系统** | DarkTheme / LightTheme 与 style 资源（Default / Dark / Light） |
| 🎨 **资源与工具** | Icons.xaml 图标资源、SkinHandler / LanguageHandler / IconsHandler、WinForm / WPF 注入辅助 |

## 📦 安装方式

通过 NuGet 获取（`Snet.Windows.Controls` 会自动引入 `Snet.Windows.Core` 依赖）：

```bash
dotnet add package Snet.Windows.Controls
```

## 🔗 依赖库

| 库 | 版本 | 用途 |
|------|------|------|
| 🖥️ [WPF-UI](https://github.com/lepoco/wpfui) | 4.3.0 | 菜单等现代化控件 |
| 🎨 [MaterialDesignThemes](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit) | 5.3.2 | 整体 UI 风格与控件支持 |
| 🧩 [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | 8.4.2 | MVVM 框架 |
| 🛠️ Snet.Core | 26.222.1 | 内部工具库（快捷方法 / 抽象类） |
| 🖥️ System.Management / System.Drawing.Common | 10.0.10 | 系统管理 / 图形支持 |

## 🙏 致谢

- 🌐 [Snet.cn](https://snet.cn)
- 🖥️ [WPF-UI](https://github.com/lepoco/wpfui)
- 🎨 [MaterialDesignInXAML](https://github.com/MaterialDesignInXamlToolkit)
- 🧩 [CommunityToolkit](https://github.com/CommunityToolkit/dotnet)

## 📜 许可证

![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)

本项目基于 **MIT** 开源协议 —— 自由使用、修改、分发。

📄 完整条款请阅读 [LICENSE](LICENSE) 文件。

> ⚠️ 软件按「原样」提供，作者不对使用后果承担责任。

## 📈 Star History

<a href="https://www.star-history.com/?repos=shunnet%2FWpfMUI&type=date&legend=bottom-right">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=shunnet/WpfMUI&type=date&theme=dark&legend=bottom-right"/>
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=shunnet/WpfMUI&type=date&legend=bottom-right"/>
   <img alt="Star History Chart" src="https://api.star-history.com/chart?repos=shunnet/WpfMUI&type=date&legend=bottom-right"/>
 </picture>
</a>
