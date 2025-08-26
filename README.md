# <img src="https://api.shunnet.top/pic/shun.png" height=24> WpfMUI

一个基于 **WPF** 的自定义界面库，内置常用控件与界面基础架构，支持快速开发。  
主要包含两个库：

## 📦 库结构

- **Snet.Windows.Core**  
  核心界面库，封装了所有基础界面操作与公共逻辑。

- **Snet.Windows.Controls**  
  控件库，继承自 `Snet.Windows.Core`，目前已集成以下自定义控件：
  - 自定义按钮
  - 图表
  - 消息框
  - 属性展示框  
  后续会持续新增更多控件。

## ✨ 特性

- 支持 **视图与视图模型绑定注入**
- 提供常用 UI 组件，开箱即用
- 可扩展性强，方便集成更多自定义控件

## 🔗 引用库

本项目依赖以下开源库：

1. [MaterialDesignThemes](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)  
   提供整体模板颜色与控件支持

2. [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)  
   提供数据绑定与 MVVM 支持

3. **Snet.Core**  
   内部工具库，包含快捷方法与抽象类

4. [ScottPlot.WPF](https://github.com/ScottPlot/ScottPlot)  
   高性能 WPF 图表库

5. [WPF-UI](https://github.com/lepoco/wpfui)  
   使用其中的菜单控件

6. [Microsoft.Web.WebView2](https://learn.microsoft.com/en-us/microsoft-edge/webview2/)  
   网页加载支持

### NuGet
```
> dotnet add package Snet.Windows.Controls
```

### 致谢
https://shunnet.top \
https://github.com/scottplot/scottplot \
https://github.com/lepoco/wpfui \
https://github.com/CommunityToolkit/dotnet \
https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit

### 许可证

![许可证：GPL v3或更高版本](https://img.shields.io/badge/License-GPL_v3_or_later-blue)  
请阅读 [LICENSE](LICENSE.txt) 完整许可证文本的文件。 \
本软件按“原样”提供，不提供任何形式的保修。 \
作者对因使用该软件而产生的任何损害不承担责任。

### [演示地址（点击跳转）](https://Shunnet.top/7EUf6)