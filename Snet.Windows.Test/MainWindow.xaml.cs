using Microsoft.Win32;
using Snet.Core.handler;
using Snet.Windows.Controls.drag;
using Snet.Windows.Controls.handler;
using Snet.Windows.Core;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Snet.Windows.Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : WindowBase
    {
        /// <summary>拖拽演示是否已初始化（页面切换回来时无需重复初始化）</summary>
        private bool dragInitialized;

        /// <summary>拖拽动画实例（DragControlsAnimate，内置副本右键菜单：固定/解除固定/提取/移除）</summary>
        private DragControlsAnimate? dragAnimate;

        /// <summary>画布上的拖动源控件（清空画布时保留）</summary>
        private readonly List<FrameworkElement> dragSources = [];

        /// <summary>JSON 布局序列化选项</summary>
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public MainWindow()
        {
            InitializeComponent();

            // 代码编辑器初始化（参考 Daq Console：EditHandler 关键字高亮 + 补全 + 悬停提示，
            // color 元组为背景色：(深色主题背景, 浅色主题背景)，皮肤切换时自动跟随）
            _ = new EditHandler(edit, App.EditModels, color: ("#454545", "#FEFEFE"));
        }

        /// <summary>取本地化字符串</summary>
        private string Loc(string key) => App.LanguageOperate.GetLanguageValue(key) ?? key;

        /// <summary>
        /// 拖拽演示页首次显示时初始化：创建 DragControlsAnimate（新库接口：直接传入控件集合初始化）。<br/>
        /// 说明：控件离开/回到可视树原样保留（元素与事件都在），仅首次需要初始化。
        /// </summary>
        private void DragPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (dragInitialized) return;
            dragInitialized = true;

            // 画布初始尺寸与窗口一致（DragControlsAnimate 在窗口尺寸变化时会自动同步宽高）
            DragCanvas.Width = ActualWidth;
            DragCanvas.Height = ActualHeight;

            // 左侧面板：自定义控件 + 原生控件混合（对象直接传入，由库注册为拖动源；
            // Name 即“控件源名称”——布局 JSON 保存 SourceName，加载时按源名称实例化源控件）
            var sources = new List<FrameworkElement>
            {
                new Button { Name = "NativeButton", Content = Loc("按钮控件"), Width = 130, Height = 32 },
                new CheckBox { Name = "NativeCheck", Content = Loc("勾选标记"), Width = 130, Height = 28 },
                new TextBox { Name = "NativeText", Text = Loc("文本框"), Width = 130, Height = 28 },
                new ComboBox { Name = "NativeCombo", Width = 130, Height = 28, ItemsSource = new[] { "选项A", "选项B", "选项C" }, SelectedIndex = 0 },
                new Rectangle { Name = "NativeRect", Width = 90, Height = 60, Fill = new SolidColorBrush(Color.FromRgb(0x2E, 0x8B, 0x57)), RadiusX = 6, RadiusY = 6 },
                new Snet.Windows.Controls.button.ButtonControl { Name = "CustomButton", Content = Loc("自定义按钮"), Width = 130, Height = 32 },
                new Snet.Windows.Controls.textbox.TextBoxControl { Name = "CustomText", Text = Loc("自定义文本框"), Width = 130, Height = 30 },
                new Snet.Windows.Controls.ledgauge.LedGaugeControl { Name = "Led", Width = 60, Height = 60, OffLightness = 0.1, IsOn = true },
            };
            dragSources.AddRange(sources);
            foreach (var source in sources)
            {
                source.Margin = new Thickness(3);
                DragPalette.Children.Add(source);
            }

            // 新库接口：构造函数直接传入控件集合，一步完成初始化（克隆工厂默认 DefaultClone）
            dragAnimate = new DragControlsAnimate(this, DragCanvas, sources);

            // 拖拽触发：每次拖拽前刷新画布在窗口中的实际偏移（保证落点与鼠标一致），
            // 克隆使用库默认工厂（按类型新建 + 复制 Content/Text/IsChecked/Color/尺寸）
            dragAnimate.DragEvenTrigger = showControl =>
            {
                var origin = DragCanvas.TranslatePoint(new Point(0, 0), this);
                dragAnimate.DynamicUpdateOffset(origin.Y, origin.X);

                var clone = DragControlsAnimate.DefaultClone(showControl);
                return (clone, CkDragMove.IsChecked == true, CkDragResize.IsChecked == true, CkDragRotate.IsChecked == true);
            };

            // 消息回调：拖拽大小启用情况 / 固定 / 提取 / 移除等，显示在消息记录框
            dragAnimate.MessageEvenTrigger = (message, _) =>
            {
                LogDragMessage(message);
                // 源面板自愈：任何拖拽操作结束后，核对待面板源控件的归属/可见性，
                // 缺失（被移出/被压成 0 高）立即回插并恢复可见 —— 左侧源永远原样存在
                EnsurePaletteSources();
            };
            // 松开鼠标时：自愈一次（仅回插/恢复可见性，不替换源实例）
            DragPalette.PreviewMouseLeftButtonUp += (_, _) => EnsurePaletteSources();

            // 源面板守护：每秒核查源控件状态，被移出/压塌立即恢复
            var sourceGuard = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            sourceGuard.Tick += (_, _) =>
            {
                foreach (var source in dragSources)
                {
                    if (source == null) continue;
                    if (!source.IsVisible || source.ActualWidth <= 1 || source.ActualHeight <= 1)
                    {
                        source.Visibility = Visibility.Visible;
                        source.InvalidateMeasure();
                        source.InvalidateArrange();
                        source.UpdateLayout();
                    }
                }
                EnsurePaletteSources();
            };
            sourceGuard.Start();
        }

        /// <summary>
        /// 源面板自愈：确保所有拖动源都在 DragPalette 中且可见。<br/>
        /// 每次拖拽消息后调用；若个别源曾被移出面板或布局压成 0 尺寸，立即恢复。
        /// </summary>
        private void EnsurePaletteSources()
        {
            try
            {
                foreach (var source in dragSources)
                {
                    if (source == null) continue;
                    if (!DragPalette.Children.Contains(source))
                    {
                        // 回插（WPF Panel.Add 会自动把源从旧父容器搬回，源对象引用不变）
                        DragPalette.Children.Add(source);
                    }
                    source.Visibility = Visibility.Visible;
                    if (source.ActualHeight <= 0 && source.Height > 0)
                    {
                        source.InvalidateMeasure();
                        source.InvalidateVisual();
                    }
                }
            }
            catch
            {
                // 自愈失败不阻断拖拽主流程
            }
        }

        /// <summary>判断元素是否为拖出的副本（非拖动源、非提示文本）</summary>
        private bool IsCopy(FrameworkElement el)
            => !dragSources.Contains(el) && !ReferenceEquals(el, DragHint);

        /// <summary>清空画布：移除所有副本，保留拖动源与提示</summary>
        private void DragClear_Click(object sender, RoutedEventArgs e)
        {
            foreach (var child in DragCanvas.Children.OfType<FrameworkElement>().Where(IsCopy).ToList())
            {
                dragAnimate?.RemoveCopy(child);
            }
        }

        /// <summary>保存布局：画布内所有副本序列化为 JSON（类型/坐标/尺寸/内容）</summary>
        private void DragSaveLayout_Click(object sender, RoutedEventArgs e)
        {
            var layout = new DragControlsLayout();
            foreach (var el in DragCanvas.Children.OfType<FrameworkElement>().Where(IsCopy))
            {
                layout.Items.Add(BuildLayoutItem(el));
            }

            var dlg = new SaveFileDialog
            {
                Filter = "JSON 布局文件 (*.json)|*.json",
                FileName = "canvas-layout.json",
                Title = Loc("保存布局")
            };
            if (dlg.ShowDialog(this) != true) return;

            try
            {
                File.WriteAllText(dlg.FileName, JsonSerializer.Serialize(layout, JsonOptions));
                LogDragMessage($"{Loc("布局已保存")}: {dlg.FileName}（{layout.Items.Count} 项）");
            }
            catch (Exception ex)
            {
                LogDragMessage($"{Loc("布局加载失败")}: {ex.Message}");
            }
        }

        /// <summary>加载布局：读取 JSON，先清空现有副本再重建（含坐标/尺寸/内容/右键菜单）</summary>
        private void DragLoadLayout_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "JSON 布局文件 (*.json)|*.json",
                Title = Loc("加载布局")
            };
            if (dlg.ShowDialog(this) != true) return;

            DragControlsLayout? layout;
            try
            {
                layout = JsonSerializer.Deserialize<DragControlsLayout>(File.ReadAllText(dlg.FileName));
            }
            catch (Exception ex)
            {
                LogDragMessage($"{Loc("布局加载失败")}: {ex.Message}");
                return;
            }
            if (layout?.Items == null || layout.Items.Count == 0)
            {
                LogDragMessage($"{Loc("布局加载失败")}: 空布局");
                return;
            }

            foreach (var old in DragCanvas.Children.OfType<FrameworkElement>().Where(IsCopy).ToList())
            {
                dragAnimate?.RemoveCopy(old);
            }

            int okCount = 0;
            foreach (var item in layout.Items)
            {
                // 优先按“控件源名称”从左侧注册的源实例化（更稳定）；找不到再按类型全名兜底
                FrameworkElement? copy = dragAnimate?.CreateBySourceName(item.SourceName) ?? CreateCopyFromLayout(item);
                if (copy is null) continue;

                // 统一应用位置/尺寸（CreateBySourceName 只克隆控件，不设置 X/Y/宽高）
                Canvas.SetLeft(copy, item.X);
                Canvas.SetTop(copy, item.Y);
                if (!double.IsNaN(item.Width)) copy.Width = item.Width;
                if (!double.IsNaN(item.Height)) copy.Height = item.Height;

                DragCanvas.Children.Add(copy);
                dragAnimate?.MoveAndDragSizeInsert(copy, this, CkDragMove.IsChecked == true, CkDragResize.IsChecked == true, CkDragRotate.IsChecked == true);
                dragAnimate?.AttachCopyMenu(copy);   // 挂内置右键菜单
                dragAnimate?.SetRotation(copy, item.Angle);   // 恢复旋转角度
                DragControlsAnimate.SetSN(copy, item.SN);
                DragControlsAnimate.SetExtensionData(copy, item.ExtensionData);
                okCount++;
            }
            LogDragMessage($"{Loc("布局已加载")}: {dlg.FileName}（{okCount}/{layout.Items.Count} 项）");
        }

        /// <summary>将副本转成布局条目（源名称/SN/扩展数据/坐标/尺寸/内容/角度）</summary>
        private DragControlsLayoutItem BuildLayoutItem(FrameworkElement el)
        {
            var item = new DragControlsLayoutItem
            {
                Type = el.GetType().FullName ?? el.GetType().Name,
                SourceName = DragControlsAnimate.GetSourceName(el),
                SN = DragControlsAnimate.GetSN(el),
                ExtensionData = DragControlsAnimate.GetExtensionData(el),
                X = Canvas.GetLeft(el),
                Y = Canvas.GetTop(el),
                Width = double.IsNaN(el.Width) ? el.ActualWidth : el.Width,
                Height = double.IsNaN(el.Height) ? el.ActualHeight : el.Height,
                Angle = dragAnimate?.GetRotation(el) ?? 0,
                Text = GetDisplayText(el),
            };
            if (el is ToggleButton toggle)
            {
                item.IsChecked = toggle.IsChecked;
            }
            else if (el.GetType().GetProperty("IsChecked") is { } ic && ic.CanRead && ic.PropertyType == typeof(bool?))
            {
                item.IsChecked = ic.GetValue(el) as bool?;
            }
            if (el is Shape shape)
            {
                item.Fill = shape.Fill?.ToString();
            }
            return item;
        }

        /// <summary>
        /// 取控件显示文本。<br/>
        /// 顺序：类型 Content 依赖属性（直接读 DP 槽位，与模板绑定一致）→ ContentControl.Content → Text 属性；仅接受字符串。
        /// </summary>
        private static string? GetDisplayText(FrameworkElement el)
        {
            try
            {
                var type = el.GetType();
                if (type.GetField("ContentProperty")?.GetValue(null) is DependencyProperty contentDp
                    && el.GetValue(contentDp) is string s1)
                {
                    return s1;
                }
                if (el is ContentControl contentControl && contentControl.Content is string s2)
                {
                    return s2;
                }
                if (type.GetProperty("Text") is { } textProp && textProp.CanRead
                    && textProp.GetValue(el) is string s3)
                {
                    return s3;
                }
            }
            catch
            {
                // 忽略
            }
            return null;
        }

        /// <summary>按布局条目创建副本：按类型全名动态创建（自定义/原生控件通吃，要求有无参构造函数）</summary>
        private static FrameworkElement? CreateCopyFromLayout(DragControlsLayoutItem item)
        {
            if (ResolveType(item.Type) is not { } type) return null;

            FrameworkElement? el;
            try
            {
                el = Activator.CreateInstance(type) as FrameworkElement;
            }
            catch
            {
                return null;
            }
            if (el == null) return null;

            if (!double.IsNaN(item.Width)) el.Width = item.Width;
            if (!double.IsNaN(item.Height)) el.Height = item.Height;

            ApplyText(el, item.Text);
            if (item.IsChecked is { } checkedValue)
            {
                if (el is ToggleButton toggle)
                {
                    toggle.IsChecked = checkedValue;
                }
                else if (el.GetType().GetProperty("IsChecked") is { } prop && prop.CanWrite && prop.PropertyType == typeof(bool?))
                {
                    prop.SetValue(el, checkedValue);
                }
            }
            if (item.Fill != null && el is Shape shape)
            {
                shape.Fill = ParseBrush(item.Fill);
            }

            Canvas.SetLeft(el, item.X);
            Canvas.SetTop(el, item.Y);
            return el;
        }

        /// <summary>按类型全名从已加载程序集解析类型</summary>
        private static Type? ResolveType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetType(typeName, throwOnError: false) is { } type)
                {
                    return type;
                }
            }
            return null;
        }

        /// <summary>
        /// 应用文本（与 GetDisplayText 对称）：<br/>
        /// 优先类型 Content 依赖属性（string 类型直接写 DP 槽位）；再 ContentControl.Content；最后 Text（反射）。
        /// </summary>
        private static void ApplyText(FrameworkElement el, string? text)
        {
            if (string.IsNullOrEmpty(text)) return;
            try
            {
                var type = el.GetType();
                if (type.GetField("ContentProperty")?.GetValue(null) is DependencyProperty contentDp
                    && contentDp.PropertyType == typeof(string))
                {
                    el.SetValue(contentDp, text);
                    return;
                }
                if (el is ContentControl contentControl && contentControl.Content is string)
                {
                    contentControl.Content = text;
                    return;
                }
                if (type.GetProperty("Text") is { } textProp && textProp.CanWrite
                    && textProp.PropertyType.IsInstanceOfType(text))
                {
                    textProp.SetValue(el, text);
                }
            }
            catch
            {
                // 忽略
            }
        }

        /// <summary>解析颜色字符串，失败时回退为 SeaGreen</summary>
        private static Brush ParseBrush(string? text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                try
                {
                    return (Brush)new BrushConverter().ConvertFromString(text);
                }
                catch
                {
                    // 忽略非法颜色，使用默认色
                }
            }
            return new SolidColorBrush(Color.FromRgb(0x2E, 0x8B, 0x57));
        }

        /// <summary>追加拖拽消息到日志框</summary>
        private void LogDragMessage(string message)
        {
            DragMessageBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            DragMessageBox.ScrollToEnd();
        }
    }
}
