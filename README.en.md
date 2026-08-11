<h1 align="center">🎨 Snet.WpfMUI</h1>

<p align="center">
  <img width="120" height="120" src="https://api.snet.cn/pic/nuget.png" alt="Snet Logo"/>
</p>

<p align="center">
  <b>A modern WPF UI library — 50+ ready-to-use controls and an application foundation, plug-and-play</b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-blue?logo=dotnet"/>
  <img src="https://img.shields.io/badge/.NET-10.0-blue?logo=dotnet"/>
  <img src="https://img.shields.io/badge/license-MIT-green"/>
  <img src="https://img.shields.io/nuget/v/Snet.Windows.Controls?color=blue"/>
  <img src="https://img.shields.io/github/stars/shunnet/WpfMUI?style=social"/>
</p>

<p align="center">
  <a href="https://snet.cn"><b>🌐 Website</b></a> ·
  <a href="https://github.com/shunnet/WpfMUI"><b>📦 GitHub</b></a> ·
  <a href="https://snet.cn/7EUf6"><b>🎬 Demo</b></a>
</p>

<p align="center">
  English | 📖 <a href="README.md"><b>简体中文</b></a>
</p>

## ✨ Highlights

| Feature | Description |
|---------|-------------|
| 🔗 **MVVM Support** | View/ViewModel binding & injection (BindNotify / EventCommand / RoutedEventTrigger) |
| 🧩 **50+ Controls** | Basic controls, data display, text editing, dialogs and system integration |
| 🌐 **Localization Engine** | Full ResX localization support (LocalizeDictionary / LocExtension, etc.) |
| 🌓 **Theming** | Dark / Light themes, charts and controls follow the skin |
| 🪟 **WindowBase** | Base window handling themes, icons, skins and language |
| 🔧 **Extensibility** | Easy to extend with custom controls |

## 🧩 Controls — Snet.Windows.Controls

| Category | Controls |
|----------|----------|
| 🎯 **Basic** | ButtonControl · ComboBoxControl · TextBoxControl · CheckMark · RadioButtonList · SpinControl · SliderEx, etc. |
| 📊 **Data Display** | DataGrid · TreeListBox · PropertyControl / PropertyGrid · LedGaugeControl · PageBarControl · ColorPicker · HeaderedEntrySlider |
| 📝 **Text Editing** | TextEditor (full AvalonEdit port: syntax highlighting / folding / code completion / search / snippets) · FormattingTextBox · EditableTextBlock |
| 💬 **Dialogs** | MessageBox (OK / OKCancel / Yes / YesNo) · PropertyDialog · WizardDialog · BrowseForFolderDialog |
| 🚀 **System Integration** | NotifyIcon (system tray + TrayManager) · DragControls (drag animations) · DockPanelSplitter |
| 🧩 **Extensions & Tools** | TextBlockEx · TextBoxEx · LinkBlock · EnumMenuItem · ButtonChrome · FilePicker / DirectoryPicker · DataAnnotations (60+ validation attributes) · Converters (26+) |

## 🏗️ Core — Snet.Windows.Core

| Module | Description |
|--------|-------------|
| 🪟 **WindowBase** | Base window class handling themes / icons / skins / language |
| 🧩 **MVVM Helpers** | BindNotify (notification base), EventCommand, RoutedEventTrigger |
| 🌐 **Localization Engine** | LocalizeDictionary, LocExtension / BLoc / FELoc, ResxLocalizationProvider |
| 🌓 **Theme System** | DarkTheme / LightTheme with style resources (Default / Dark / Light) |
| 🎨 **Resources & Tools** | Icons.xaml, SkinHandler / LanguageHandler / IconsHandler, WinForm / WPF injection helpers |

## 📦 Installation

Get it from NuGet (`Snet.Windows.Controls` brings in `Snet.Windows.Core` automatically):

```bash
dotnet add package Snet.Windows.Controls
```

## 🔗 Dependencies

| Library | Version | Purpose |
|---------|---------|---------|
| 🖥️ [WPF-UI](https://github.com/lepoco/wpfui) | 4.3.0 | Modern controls such as menus |
| 🎨 [MaterialDesignThemes](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit) | 5.3.2 | Overall UI style and control support |
| 🧩 [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | 8.4.2 | MVVM framework |
| 🛠️ Snet.Core | 26.222.1 | Internal utility library (helper methods / abstract classes) |
| 🖥️ System.Management / System.Drawing.Common | 10.0.10 | System management / graphics support |

## 🙏 Acknowledgements

- 🌐 [Snet.cn](https://snet.cn)
- 🖥️ [WPF-UI](https://github.com/lepoco/wpfui)
- 🎨 [MaterialDesignInXAML](https://github.com/MaterialDesignInXamlToolkit)
- 🧩 [CommunityToolkit](https://github.com/CommunityToolkit/dotnet)

## 📜 License

![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)

This project is licensed under the **MIT** License — free to use, modify and distribute.

📄 See the [LICENSE](LICENSE) file for the full terms.

> ⚠️ The software is provided "as is", without warranty of any kind.

## 📈 Star History

<a href="https://www.star-history.com/?repos=shunnet%2FWpfMUI&type=date&legend=bottom-right">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=shunnet/WpfMUI&type=date&theme=dark&legend=bottom-right&sealed_token=urcaATW4Hc7ZJfh-ABg8JSIplISwOoHIUv23AhRfmQQfX5LG8uJX404fQ1F4yXVGifaSzp55kmKLKmJO7tBsFlZGu5Lu4Zr3TSm8VU2kEVETu1uAKtDS3Q" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=shunnet/WpfMUI&type=date&legend=bottom-right&sealed_token=urcaATW4Hc7ZJfh-ABg8JSIplISwOoHIUv23AhRfmQQfX5LG8uJX404fQ1F4yXVGifaSzp55kmKLKmJO7tBsFlZGu5Lu4Zr3TSm8VU2kEVETu1uAKtDS3Q" />
   <img alt="Star History Chart" src="https://api.star-history.com/chart?repos=shunnet/WpfMUI&type=date&legend=bottom-right&sealed_token=urcaATW4Hc7ZJfh-ABg8JSIplISwOoHIUv23AhRfmQQfX5LG8uJX404fQ1F4yXVGifaSzp55kmKLKmJO7tBsFlZGu5Lu4Zr3TSm8VU2kEVETu1uAKtDS3Q" />
 </picture>
</a>
