# 画布拖拽调整（DragControls）说明与示例

```
动画拖动，包含控件的移动与缩放大小
by:Snet.cn 2022/6/8   （2026 增强：鼠标捕获、落点偏移修正、右键菜单、JSON 布局持久化）
```

---

## 1. 功能概览

| 类 | 作用 |
|---|---|
| `DragControlsAnimate` | 拖拽动画：拖动过程中显示副本（半透明跟随鼠标），松手后**副本保留在拖拽终点**，并可通过周围点继续 移动 / 缩放；副本**内置右键菜单**（固定/解除固定/提取/移除） |
| `DragControlsExcessiveAnimate` | 过度动画：拖动过程显示半透明副本，松手后**渐隐消失**（拖拽预览） |
| `DragControlsBase` | 装饰器基类（Adorner）：4 个边 + 4 个角的拖拽点用于缩放，中心点拖动整个控件，**顶部旋转手柄**（绕中心旋转，独立圆点无连接线） |
| `DragControlsHelper` | 装饰器层管理：`Insert` / `Remove`（添加/移除移动与拖拽大小功能），内部字典防重复 |
| `DragControlsLayout` / `DragControlsLayoutItem` | 布局 JSON 序列化模型（System.Text.Json），保存/还原整个画布布局 |

参考完整实现：`Snet.Windows.Test` → 「拖拽控件」页（`MainWindow.xaml` / `MainWindow.xaml.cs`）。

---

## 2. 快速开始（后端）

```csharp
using Snet.Windows.Controls.drag;

DragControlsAnimate dragControlsAnimate;

public MainWindow()
{
    InitializeComponent();
    // Parameters: 窗体, 布局容器(Canvas 或 Grid)
    dragControlsAnimate = new DragControlsAnimate(this, Pane);

    // 注册拖动源：从哪个控件上按下左键拖动
    dragControlsAnimate.Insert(ConShow1);
    dragControlsAnimate.Insert(ConShow2);

    dragControlsAnimate.MessageEvenTrigger += MessageEvenTrigger;
    dragControlsAnimate.DragEvenTrigger += DragEvenTrigger;
}

/// <summary>回调消息（拖拽大小启用情况 / 控件已存在等）</summary>
public void MessageEvenTrigger(string Message, FrameworkElement element)
{
    Console.WriteLine($"控件Name:{element.Name}->抛出消息：{Message}");
}

/// <summary>
/// 拖拽触发：返回 (新控件, 是否需要移动, 是否需要拖拽大小)
/// 每次按下左键都会创建新控件，松手后留在终点
/// </summary>
public (FrameworkElement NewControl, bool IsMove, bool IsDragSize) DragEvenTrigger(FrameworkElement ShowControl)
{
    FrameworkElement NewControl = new Button
    {
        Content = ShowControl is Button b ? b.Content : "自定义控件",
        Width = 140,
        Height = 32
    };
    return (NewControl, true, true);
}
```

### 快速初始化（自定义/原生控件直接传入）

```csharp
// 左侧面板控件（自定义 + 原生混合），直接传入即可初始化：注册拖动源 + 默认克隆工厂
var sources = new List<FrameworkElement>
{
    new Button { Content = "按钮", Width = 130, Height = 32 },
    new TextBox { Text = "文本框", Width = 130, Height = 28 },
    new Snet.Windows.Controls.button.ButtonControl { Content = "自定义按钮", Width = 130, Height = 32 },
    new Snet.Windows.Controls.ledgauge.LedGaugeControl { Width = 60, Height = 60, IsOn = true },
};
foreach (var s in sources) PalettePanel.Children.Add(s);   // 左侧面板显示

var drag = new DragControlsAnimate(this, CanvasPane, sources);   // ← 新接口：一步初始化
// 也可分步：drag.Initialize(sources);  或 仅注册 drag.Insert(source)

// 默认克隆工厂：按类型新建 + 复制 Content/Text/IsChecked/Color/尺寸（要求控件有无参构造函数）；
// 需要定制时覆盖 DragEvenTrigger
```

### 前端代码

```xml
<Window x:Class="WpfApp5.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        Title="Canvas与Grid 中拖动动画+缩放+移动 Snet.cn"
        Height="500" Width="800">
    <!-- Canvas 容器（Canvas.Left/Top 定位） -->
    <Canvas Name="Pane" Background="DarkGray">
        <Button Content="Press me" Width="90" Height="50" Name="ConShow1"
                VerticalAlignment="Top" HorizontalAlignment="Left"/>
    </Canvas>
    <!-- Grid 容器（Margin 定位，依赖窗体宽高）：
    <Grid Name="Pane" Background="DarkGray">
        <Button Content="Press me" Width="90" Height="50" Name="ConShow1"
                VerticalAlignment="Top" HorizontalAlignment="Left" Margin="0,30,0,0"/>
    </Grid> -->
</Window>
```

> 注意：单个窗体中只能定义一个布局容器（每个 `DragControlsAnimate` 对应一个容器）；
> 容器会由库在窗体尺寸变化时自动同步宽高。

---

## 3. 容器偏移（容器不在窗体原点时必须设置）

拖拽落点以“窗体坐标”计算；如果容器没有填满整个窗体（例如放在标题栏/工具行/边框下方），
需要把容器在窗体中的实际偏移告诉库，否则副本落点会整体偏移：

```csharp
var origin = Pane.TranslatePoint(new Point(0, 0), this);
dragControlsAnimate.DynamicUpdateOffset(origin.Y, origin.X);
```

**建议在每次拖拽触发前刷新**（此时容器必然已布局完成）：

```csharp
dragControlsAnimate.DragEvenTrigger = showControl =>
{
    var origin = Pane.TranslatePoint(new Point(0, 0), this);
    dragControlsAnimate.DynamicUpdateOffset(origin.Y, origin.X);
    return (CreateInstance(showControl), true, true);
};
```

---

## 4. 鼠标行为（2026 增强）

- 左键按下：在源控件上 **`CaptureMouse()`** —— 拖动期间鼠标离开源控件后持续跟随，直到松开；
- 左键松开：释放捕获，副本（`DragControlsAnimate`）保留在终点 /（`DragControlsExcessiveAnimate`）渐隐消失；
- `LostMouseCapture` 兜底：捕获被其他窗口抢占（如 Alt+Tab）时自动恢复状态，副本不会卡在半透明、拖不动；
- 按下瞬间的初始定位同样应用偏移量，起始位置与鼠标一致。

---

## 5. 内置右键菜单：固定 / 解除固定 / 提取 / 移除

**菜单直接内置在 `DragControlsAnimate` 中**（无需外部定义）：拖出的副本自动挂载右键菜单，
样式参考 Daq `PluginBrowse.xaml`（WPF-UI 图标 + 控件库多语言 + 竖分割线）：

| 菜单项 | 图标 | 功能 |
|---|---|---|
| 固定 | `SymbolRegular.Pin24` | 隐藏控件周围的点，无法拖拽位置/缩放（固定后可点「调整」恢复） |
| 调整 | `SymbolRegular.Resize24` | 重新显示控件周围的点，可拖拽位置与缩放大小 |
| 提取 | `SymbolRegular.Copy24` | 将副本在画布中的 X/Y 坐标复制到剪贴板（如 `X=165, Y=104`） |
| 设置 | `SymbolRegular.Settings24` | 内置设置对话框：标识 SN（默认预填 `Guid.NewGuid()`，可改）/ 扩展数据（多行）；写入副本附加属性并随 JSON 持久化；可订阅 `SettingsRequested` 换成自定义对话框 |
| 移除 | `SymbolRegular.Delete24` | 先移除装饰器与移动/拖拽大小事件，再把副本移出容器 |

```csharp
var drag = new DragControlsAnimate(this, Pane);
drag.EnableContextMenu = true;          // 默认开启；false 则副本不挂菜单，可自行处理 ContextMenu
drag.MessageEvenTrigger = (msg, el) => Console.WriteLine(msg);   // 固定/解除固定/提取/移除 均有消息回调

// 程序化添加的副本（如 JSON 恢复）也要挂菜单：
var copy = CreateInstance(...);
Pane.Children.Add(copy);
drag.MoveAndDragSizeInsert(copy, this, true, true);
drag.AttachCopyMenu(copy);
```

- 公开 API：`AttachCopyMenu(copy)`、`PinCopy(copy)`、`UnpinCopy(copy)`、`ExtractCopy(copy)`、`RemoveCopy(copy)`；
- 副本元数据（附加属性）：`DragControlsAnimate.Get/SetSourceName / Get/SetSN / Get/SetExtensionData`（`SourceName`——布局 JSON 按源名称实例化）；
- 源名称注册：`RegisterSource(sourceName, source)` 或 `Initialize(sources)`（控件带 `Name` 自动按 Name 注册）；`CreateBySourceName(name)` 按源名称创建实例；`ContainsSourceName(name)` 判断是否存在；
- 多语言键（`Snet.Windows.Controls\Language.resx` / `Language.en.resx`）：`固定→Pin`、`调整→Adjust`、`提取→Extract`、`设置→Settings`、`控件源名称→Source Name`、`SN`、`扩展数据→Extension Data`、`移除→Remove`、`已固定→Pinned (right-click to unpin)`；
- 分割线使用 `<Separator Width="1" Style=...DefaultDragIndicatorStyleStyle...>`（WPF-UI 资源；应用未合并 WPF-UI 主题时自动退回默认样式）；
- 菜单为每个副本独立创建（状态互不影响），生命周期随副本释放。

> 各应用只需要创建 `DragControlsAnimate` 并注册拖动源即可获得完整功能；
> `Snet.Windows.Test` 的「拖拽控件」页是参考实现。

---

## 6. JSON 布局：保存 / 加载

使用 `DragControlsLayout` / `DragControlsLayoutItem`（`Snet.Windows.Controls.drag`），
借助 System.Text.Json 持久化整张画布：

```csharp
using System.Text.Json;
using Snet.Windows.Controls.drag;

static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

// ---- 保存 ----
var layout = new DragControlsLayout();
foreach (var el in Pane.Children.OfType<FrameworkElement>().Where(IsCopy))
{
    layout.Items.Add(new DragControlsLayoutItem
    {
        Type  = el is Button ? "Button" : el.GetType().Name,
        X     = Canvas.GetLeft(el),
        Y     = Canvas.GetTop(el),
        Width = el.Width,
        Height = el.Height,
        Text  = (el as Button)?.Content?.ToString() ?? (el as TextBox)?.Text
    });
}
File.WriteAllText("layout.json", JsonSerializer.Serialize(layout, JsonOptions));

// ---- 加载 ----
var loaded = JsonSerializer.Deserialize<DragControlsLayout>(File.ReadAllText("layout.json"));
foreach (var item in loaded!.Items)
{
    FrameworkElement el = item.Type switch
    {
        "Button"    => new Button    { Content = item.Text, Width = item.Width, Height = item.Height },
        "TextBox"   => new TextBox   { Text = item.Text, Width = item.Width, Height = item.Height },
        _           => new Button    { Content = item.Text, Width = item.Width, Height = item.Height }
    };
    Canvas.SetLeft(el, item.X);
    Canvas.SetTop(el, item.Y);
    Pane.Children.Add(el);
    dragControlsAnimate.MoveAndDragSizeInsert(el, this, true, true);   // 重建后同样挂上点
}
```

`DragControlsLayoutItem` 字段：`Type / X / Y / Width / Height / Text / IsChecked / Fill / Extra`（Extra 为可扩展键值对，类型不匹配时安全忽略；`Version` 字段用于后续兼容判断）。

示例生成文件：

```json
{
  "Version": 1,
  "Items": [
    { "Type": "Button", "X": 165, "Y": 104.5, "Width": 140, "Height": 32, "Text": "按钮控件", "IsChecked": null, "Fill": null, "Extra": null }
  ]
}
```

---

## 7. 性能与稳定性

- **拖拽点模板共享**：`DragControlsBase` 静态构造仅创建一次控件模板，各 Thumb 通过模板绑定复用；
- **旋转**：顶部旋转手柄（拖拽绕控件中心旋转，0°=手柄在正上方），控件与装饰器同步旋转（`RenderTransform` 视觉变换，不影响布局槽位）；API：`DragControlsAnimate.SetRotation(copy, angle)` / `GetRotation(copy)`；JSON 字段 `Angle`（度）；
- **事件成对订阅/退订**：`Insert`/`Remove`（或 `MoveAndDragSizeInsert`/`MoveAndDragSizeRemove`）成对使用，`Remove` 内部退订移动事件、移除装饰器，避免泄漏；
- **装饰器层随页面走**：演示页把拖拽画布包在 `AdornerDecorator` 中，切页时装饰器随页面一起卸载；
- **拖拽运行期零分配**：定位直接写 `Canvas.Left/Top` 或 `Margin`，无每帧对象创建；
- **副本务必走 `MoveAndDragSizeRemove` 再移除**，直接移除会残留装饰器与事件。

---

## 8. 注意事项

1. 一个窗体/容器只定义一个 `DragControlsAnimate`；容器类型二选一：`Canvas`（`Canvas.Left/Top`）或 `Grid`（`Margin`，依赖窗体宽高）；
2. 容器不要设置固定宽高（库会在窗体尺寸变化时同步宽高）；容器不在窗体原点时设置偏移（见第 3 节）；
3. 每次拖拽生成新副本（`DragEvenTrigger` 返回新实例）；两控件引用相同会导致“此控件已在布局中存在”；
4. 副本的“移动”通过拖拽点/控件主体修改位置，“拖拽大小”通过 8 个点（四边+四角）；
5. 拖动源控件本身不会被移动，移动的是拖出的副本。
