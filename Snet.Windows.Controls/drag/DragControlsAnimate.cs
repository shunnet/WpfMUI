using MaterialDesignThemes.Wpf;
using Snet.Model.data;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Snet.Windows.Controls.drag
{
    /// <summary>
    /// 拖拽控件动画 控件在拖动过程中显示，拖动完成也显示在拖动结束的位置
    /// </summary>
    public class DragControlsAnimate
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="Windows">窗体</param>
        /// <param name="LlayoutContainer">容器：让控件在这里面拖动</param>
        /// <param name="HeightOffset">高度偏移</param>
        /// <param name="WidthOffset">宽度偏移</param>
        public DragControlsAnimate(FrameworkElement Windows, object LlayoutContainer, double HeightOffset = 0, double WidthOffset = 0)
        {
            this.HeightOffset = HeightOffset;
            this.WidthOffset = WidthOffset;
            this.Windows = Windows;
            this.LlayoutContainer = LlayoutContainer;
            Windows.SizeChanged += Windwos_SizeChanged;
        }

        /// <summary>
        /// 构造函数：窗体 + 容器 + 初始化控件集合，一步完成初始化。<br/>
        /// 控件可以是自定义控件或原生 WPF 控件；克隆行为默认使用 <see cref="DefaultClone"/>，
        /// 可自行覆盖 <see cref="DragEvenTrigger"/> 定制。
        /// </summary>
        /// <param name="Windows">窗体</param>
        /// <param name="LlayoutContainer">容器：让控件在这里面拖动</param>
        /// <param name="sources">初始化控件集合（自定义/原生控件对象）</param>
        /// <param name="HeightOffset">高度偏移</param>
        /// <param name="WidthOffset">宽度偏移</param>
        public DragControlsAnimate(FrameworkElement Windows, object LlayoutContainer, IEnumerable<FrameworkElement> sources, double HeightOffset = 0, double WidthOffset = 0)
            : this(Windows, LlayoutContainer, HeightOffset, WidthOffset)
        {
            Initialize(sources);
        }

        /// <summary>
        /// 批量初始化拖动源：传入多个控件（自定义或原生），自动注册为拖动源。<br/>
        /// 若尚未设置 <see cref="DragEvenTrigger"/>，自动绑定默认克隆工厂 <see cref="DefaultClone"/>。
        /// </summary>
        /// <param name="sources">控件集合（自定义/原生控件对象）</param>
        public void Initialize(IEnumerable<FrameworkElement> sources)
        {
            if (sources == null) return;
            foreach (var source in sources)
            {
                if (source == null) continue;
                Insert(source);
                // 控件带 Name 时自动按 Name 注册源名称（布局加载时按源名称实例化）
                if (!string.IsNullOrWhiteSpace(source.Name))
                {
                    SetSourceName(source, source.Name);
                    sourceRegistry[source.Name] = source;
                }
            }
            DragEvenTrigger ??= source => (DefaultClone(source), true, true, true);
        }

        /// <summary>
        /// 默认克隆工厂：按源控件实际类型创建全新实例，并复制拖拽场景所需的常用属性。
        /// <para>
        /// 注意：该方法不会复用源控件实例。
        /// 对 WPF Freezable 类型属性会创建独立副本，避免源控件与克隆控件共享可变对象。
        /// </para>
        /// </summary>
        /// <param name="source">源控件</param>
        /// <returns>全新的控件实例</returns>
        /// <exception cref="ArgumentNullException">源控件为空</exception>
        /// <exception cref="InvalidOperationException">无法创建控件实例</exception>
        public static FrameworkElement DefaultClone(FrameworkElement source)
        {
            ArgumentNullException.ThrowIfNull(source);

            var type = source.GetType();

            FrameworkElement clone;

            try
            {
                clone = Activator.CreateInstance(type) as FrameworkElement
                    ?? throw new InvalidOperationException(
                        $"类型 {type.FullName} 创建结果不是 FrameworkElement。");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"无法创建控件副本：{type.FullName}。请确保控件具有可访问的无参构造函数。",
                    ex);
            }

            // 尺寸
            if (!double.IsNaN(source.Width))
                clone.Width = source.Width;

            if (!double.IsNaN(source.Height))
                clone.Height = source.Height;

            // 常用属性
            CopyCommonProperty(
                source,
                clone,
                nameof(ContentControl.Content));

            CopyCommonProperty(
                source,
                clone,
                "Text");

            CopyCommonProperty(
                source,
                clone,
                "IsChecked");

            CopyCommonProperty(
                source,
                clone,
                "Color");

            CopyCommonProperty(
                source,
                clone,
                "Fill");

            // ComboBox
            if (source is ComboBox combo &&
                clone is ComboBox comboClone)
            {
                CopyComboBoxItems(combo, comboClone);
            }

            return clone;
        }
        /// <summary>
        /// 复制 ComboBox 的项目。
        /// 优先复制当前项目值；不直接复用源控件的 Items 集合。
        /// </summary>
        private static void CopyComboBoxItems(
            ComboBox source,
            ComboBox target)
        {
            try
            {
                // ItemsSource 存在时不能直接修改 target.Items。
                // 这里保持原数据源引用，避免擅自深拷贝未知业务对象。
                if (source.ItemsSource != null)
                {
                    target.ItemsSource = source.ItemsSource;
                }
                else
                {
                    foreach (var item in source.Items)
                    {
                        target.Items.Add(item);
                    }
                }

                if (source.SelectedIndex >= 0)
                {
                    target.SelectedIndex = source.SelectedIndex;
                }
            }
            catch
            {
                // ComboBox 复制失败不影响其他控件属性
            }
        }
        /// <summary>
        /// 按属性名从源控件复制到目标控件。
        /// <para>
        /// 优先复制同名依赖属性；对于引用类型值，尽可能创建独立副本，
        /// 避免源控件与克隆控件共享可变对象。
        /// </para>
        /// </summary>
        private static void CopyCommonProperty(
            object source,
            object target,
            string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return;

            try
            {
                // 1. 优先处理 DependencyProperty
                if (source is DependencyObject sourceDp &&
                    target is DependencyObject targetDp &&
                    source.GetType().GetField(
                        propertyName + "Property",
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Static |
                        BindingFlags.FlattenHierarchy) is { } field &&
                    field.GetValue(null) is DependencyProperty dp)
                {
                    var value = sourceDp.GetValue(dp);

                    if (value != null)
                    {
                        try
                        {
                            targetDp.SetValue(dp, ClonePropertyValue(value));
                            return;
                        }
                        catch
                        {
                            // DP 类型不匹配，继续尝试普通属性
                        }
                    }
                }

                // 2. 普通 CLR 属性
                var sourceProperty = source.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);

                if (sourceProperty == null || !sourceProperty.CanRead)
                    return;

                var targetProperty = target.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);

                if (targetProperty == null || !targetProperty.CanWrite)
                    return;

                if (!targetProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
                    return;

                var value2 = sourceProperty.GetValue(source);

                if (value2 == null)
                    return;

                targetProperty.SetValue(
                    target,
                    ClonePropertyValue(value2));
            }
            catch
            {
                // 单个属性复制失败不影响整个克隆流程
            }
        }

        /// <summary>
        /// 克隆属性值。
        /// WPF 的 Freezable（Brush、Transform、Geometry 等）必须避免直接共享。
        /// 其他类型默认直接返回原引用，由具体控件自行决定其生命周期。
        /// </summary>
        private static object? ClonePropertyValue(object? value)
        {
            if (value == null)
                return null;

            // WPF 可冻结对象：创建独立副本
            if (value is Freezable freezable)
            {
                try
                {
                    return freezable.Clone();
                }
                catch
                {
                    return value;
                }
            }

            return value;
        }
        #region 私有字段
        /// <summary>
        /// 高度偏移
        /// 拖动过度时，已经显示了要拖动的控件[半透明状态]，鼠标默认是在中心位置，由于窗体样式原因，导致不在中心点位置，所以加上了偏移
        /// </summary>
        double HeightOffset { get; set; }
        /// <summary>
        /// 宽度偏移
        /// 拖动过度时，已经显示了要拖动的控件[半透明状态]，鼠标默认是在中心位置，由于窗体样式原因，导致不在中心点位置，所以加上了偏移
        /// </summary>
        double WidthOffset { get; set; }
        /// <summary>
        /// 界面上已经生成的控件，也就是从哪个控件上拖动的集合
        /// </summary>
        readonly List<FrameworkElement> ShowControlsList = new List<FrameworkElement>();
        /// <summary>
        /// 窗体
        /// </summary>
        readonly FrameworkElement Windows;
        /// <summary>
        /// 容器：让控件在这里面拖动
        /// </summary>
        readonly object LlayoutContainer;
        /// <summary>
        /// 鼠标是否按下
        /// </summary>
        bool IsMouseDown = false;
        /// <summary>
        /// 实时需要拖动的控件
        /// </summary>
        FrameworkElement ControlsObj;
        /// <summary>
        /// 拖拽大小与移动
        /// </summary>
        readonly DragControlsHelper dragControlsHelper = new DragControlsHelper();

        /// <summary>
        /// 已固定（隐藏周围点、禁止移动/拖拽大小）的副本集合
        /// </summary>
        readonly HashSet<FrameworkElement> fixedCopies = [];

        /// <summary>源名称 → 源控件 注册表（用于按源名称实例化控件）</summary>
        readonly Dictionary<string, FrameworkElement> sourceRegistry = [];

        #endregion

        #region 副本元数据（附加属性）：源名称 / SN / 扩展数据

        /// <summary>控件源名称（附加属性）：标识该副本由哪个源控件克隆而来</summary>
        public static readonly DependencyProperty SourceNameProperty =
            DependencyProperty.RegisterAttached("SourceName", typeof(string), typeof(DragControlsAnimate), new PropertyMetadata(null));

        public static string? GetSourceName(DependencyObject d) => (string?)d.GetValue(SourceNameProperty);

        public static void SetSourceName(DependencyObject d, string? value) => d.SetValue(SourceNameProperty, value);

        /// <summary>标识 SN（附加属性）</summary>
        public static readonly DependencyProperty SNProperty =
            DependencyProperty.RegisterAttached("SN", typeof(string), typeof(DragControlsAnimate), new PropertyMetadata(null));

        public static string? GetSN(DependencyObject d) => (string?)d.GetValue(SNProperty);

        public static void SetSN(DependencyObject d, string? value) => d.SetValue(SNProperty, value);

        /// <summary>扩展数据（附加属性，任意文本）</summary>
        public static readonly DependencyProperty ExtensionDataProperty =
            DependencyProperty.RegisterAttached("ExtensionData", typeof(string), typeof(DragControlsAnimate), new PropertyMetadata(null));

        public static string? GetExtensionData(DependencyObject d) => (string?)d.GetValue(ExtensionDataProperty);

        public static void SetExtensionData(DependencyObject d, string? value) => d.SetValue(ExtensionDataProperty, value);

        #endregion

        #region 源名称注册

        /// <summary>
        /// 注册带源名称的拖动源：源名称用于布局 JSON 的 SourceName 持久化，
        /// 布局加载时按源名称实例化对应源控件。<br/>
        /// 左侧面板请调用本方法注册（而不是 Insert）。
        /// </summary>
        /// <param name="sourceName">源名称（唯一标识）</param>
        /// <param name="source">源控件（显示在左侧面板）</param>
        public void RegisterSource(string sourceName, FrameworkElement source)
        {
            if (source == null || string.IsNullOrWhiteSpace(sourceName)) return;
            SetSourceName(source, sourceName);
            sourceRegistry[sourceName] = source;
            Insert(source);
        }

        /// <summary>按源名称创建控件实例（克隆自注册的源控件；未注册则返回 null）</summary>
        /// <param name="sourceName">源名称</param>
        public FrameworkElement? CreateBySourceName(string? sourceName)
        {
            if (string.IsNullOrWhiteSpace(sourceName)) return null;
            return sourceRegistry.TryGetValue(sourceName, out var source) ? DefaultClone(source) : null;
        }

        /// <summary>是否已注册指定源名称</summary>
        public bool ContainsSourceName(string? sourceName)
            => !string.IsNullOrWhiteSpace(sourceName) && sourceRegistry.ContainsKey(sourceName);

        #endregion

        #region 内置右键菜单（固定/解除固定/提取/移除）

        /// <summary>
        /// 是否给拖出的副本启用内置右键菜单（固定/解除固定/提取/移除）。<br/>
        /// 默认开启；关闭后副本不挂菜单，可自行处理 ContextMenu。
        /// </summary>
        public bool EnableContextMenu { get; set; } = true;

        /// <summary>已挂内置右键菜单的副本集合（语言切换时重建菜单）</summary>
        private readonly List<FrameworkElement> menuCopies = [];

        /// <summary>是否已订阅语言切换事件</summary>
        private bool languageSubscribed;

        /// <summary>控件库（Snet.Windows.Controls）多语言模型，与 XAML snet:Loc 同源</summary>
        private static readonly LanguageModel Language = new("Snet.Windows.Controls", "Language", "Snet.Windows.Controls.dll");

        /// <summary>取控件库多语言字符串</summary>
        private static string Loc(string key)
            => Snet.Core.handler.LanguageHandler.GetLanguageValue(key, Language) ?? key;

        /// <summary>
        /// 订阅语言切换事件：切换中英文时重建所有副本的右键菜单（菜单文案在拖出时解析，需刷新）。
        /// </summary>
        private void EnsureLanguageSubscription()
        {
            if (languageSubscribed) return;
            languageSubscribed = true;
            Snet.Core.handler.LanguageHandler.OnLanguageEvent += LanguageHandler_OnLanguageEvent;
        }

        /// <summary>语言切换回调：在 UI 线程上重建副本菜单</summary>
        private void LanguageHandler_OnLanguageEvent(object? sender, Snet.Model.data.EventLanguageResult e)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null) return;
            dispatcher.BeginInvoke(() =>
            {
                foreach (var copy in menuCopies.ToList())
                {
                    if (fixedCopies.Contains(copy))
                    {
                        // 固定状态的弹提示同步刷新
                        copy.ToolTip = Loc("已固定");
                    }
                    copy.ContextMenu = BuildCopyContextMenu(copy);
                }
            });
        }

        /// <summary>
        /// 为副本挂内置右键菜单（EnableContextMenu 为 true 时生效）。<br/>
        /// 拖拽添加的副本自动挂载；程序化添加的副本（如 JSON 恢复）主动调用本方法即可。
        /// </summary>
        /// <param name="copy">拖出的副本控件</param>
        public void AttachCopyMenu(FrameworkElement copy)
        {
            if (!EnableContextMenu || copy == null || copy.ContextMenu != null) return;
            copy.ContextMenu = BuildCopyContextMenu(copy);
            menuCopies.Add(copy);
            EnsureLanguageSubscription();
        }

        /// <summary>
        /// 创建副本右键菜单：固定/调整、提取、移除。<br/>
        /// 样式参考 Daq PluginBrowse：WPF-UI 图标 + 控件库多语言 + 竖分割线。
        /// </summary>
        private ContextMenu BuildCopyContextMenu(FrameworkElement copy)
        {
            var menu = new ContextMenu();

            // ContextMenu 使用 Wpf.Ui 样式（UiContextMenu，与 Daq 菜单一致）；
            // 应用未合并 WPF-UI 资源时退回默认样式
            if (Application.Current?.TryFindResource("UiContextMenu") is Style uiMenuStyle)
            {
                menu.Style = uiMenuStyle;
            }

            // 固定 = 隐藏周围点（不可拖拽/缩放）；调整 = 显示周围点（可拖拽/缩放）。
            // 两项始终并列显示，不做切换
            var pinItem = new Wpf.Ui.Controls.MenuItem
            {
                Header = Loc("固定"),
                Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Pin24 }
            };
            pinItem.Click += (_, _) => PinCopy(copy);

            var adjustItem = new Wpf.Ui.Controls.MenuItem
            {
                Header = Loc("调整"),
                Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Resize24 }
            };
            adjustItem.Click += (_, _) => UnpinCopy(copy);

            var extractItem = new Wpf.Ui.Controls.MenuItem
            {
                Header = Loc("提取"),
                Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Copy24 }
            };
            extractItem.Click += (_, _) => ExtractCopy(copy);

            var settingsItem = new Wpf.Ui.Controls.MenuItem
            {
                Header = Loc("设置"),
                Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Settings24 }
            };
            settingsItem.Click += (_, _) => OnSettingsClicked(copy);

            var removeItem = new Wpf.Ui.Controls.MenuItem
            {
                Header = Loc("移除"),
                Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Delete24 }
            };
            removeItem.Click += (_, _) => RemoveCopy(copy);

            // 竖分割线（WPF-UI 样式，应用未合并 WPF-UI 资源时退回默认样式）
            var separator = new Separator { Margin = new Thickness(0, 5, 0, 5) };

            ZeroVerticalPadding(menu);
            ZeroVerticalPadding(pinItem);
            ZeroVerticalPadding(adjustItem);
            ZeroVerticalPadding(extractItem);
            ZeroVerticalPadding(settingsItem);
            ZeroVerticalPadding(removeItem);

            menu.Items.Add(pinItem);
            menu.Items.Add(adjustItem);
            menu.Items.Add(extractItem);
            menu.Items.Add(settingsItem);
            menu.Items.Add(separator);
            menu.Items.Add(removeItem);
            return menu;
        }

        /// <summary>
        /// 上下内边距/外边距归零（横向保留样式默认值），让右键菜单更紧凑。
        /// </summary>
        private static void ZeroVerticalPadding(Control control)
        {
            var padding = control.Padding;
            if (padding.Top != 0 || padding.Bottom != 0)
            {
                control.Padding = new Thickness(padding.Left, 0, padding.Right, 0);
            }
            var margin = control.Margin;
            if (margin.Top != 0 || margin.Bottom != 0)
            {
                control.Margin = new Thickness(margin.Left, 0, margin.Right, 0);
            }
        }

        /// <summary>固定：隐藏控件周围的点，无法拖拽位置/缩放大小</summary>
        /// <param name="copy">副本控件</param>
        public void PinCopy(FrameworkElement copy)
        {
            if (copy == null) return;
            fixedCopies.Add(copy);
            dragControlsHelper.Remove(copy);
            copy.ToolTip = Loc("已固定");
            MessageEvenTrigger?.Invoke($"{copy.GetType().Name}: {Loc("固定")}", copy);
        }

        /// <summary>调整：重新显示控件周围的点，可拖拽位置与缩放大小</summary>
        /// <param name="copy">副本控件</param>
        public void UnpinCopy(FrameworkElement copy)
        {
            if (copy == null) return;
            fixedCopies.Remove(copy);
            MoveAndDragSizeInsert(copy, Windows, true, true, true);
            copy.ToolTip = null;
            MessageEvenTrigger?.Invoke($"{copy.GetType().Name}: {Loc("调整")}", copy);
        }

        /// <summary>
        /// 设置菜单点击时触发；若未订阅，则显示库内置设置对话框（源名称/SN/扩展数据）。
        /// </summary>
        public event Action<FrameworkElement>? SettingsRequested;

        /// <summary>
        /// 设置对话框返回值
        /// </summary>
        private sealed class SettingsResult
        {
            public string? SN;
            public string? ExtensionData;
        }

        /// <summary>设置菜单点击入口</summary>
        private void OnSettingsClicked(FrameworkElement copy)
        {
            if (SettingsRequested != null)
            {
                SettingsRequested(copy);
                return;
            }
            _ = ShowSettingsDialogAsync(copy);
        }

        /// <summary>
        /// 库内置设置对话框：控件源名称（只读）/ 标识 SN / 扩展数据（多行）。
        /// 通过 DialogHost 的 CloseDialogCommand 提交，写入副本的附加属性。
        /// </summary>
        private async Task ShowSettingsDialogAsync(FrameworkElement copy)
        {
            try
            {
                var content = new StackPanel { Margin = new Thickness(20), MinWidth = 300 };

                content.Children.Add(new TextBlock
                {
                    Text = Loc("设置"),
                    FontSize = 15,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                // SN 默认数据：已设置的 SN 或新生成 GUID
                var snBox = new TextBox { Text = GetSN(copy) ?? Guid.NewGuid().ToString() };
                var extBox = new TextBox { Text = GetExtensionData(copy) ?? string.Empty, Height = 80, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
                AddField(content, Loc("SN"), snBox);
                AddField(content, Loc("扩展数据"), extBox);

                var row = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 16, 0, 0)
                };
                var ok = new Button { Content = Loc("确认"), MinWidth = 80, Margin = new Thickness(0, 0, 8, 0) };
                var cancel = new Button { Content = Loc("取消"), MinWidth = 80 };
                row.Children.Add(ok);
                row.Children.Add(cancel);
                content.Children.Add(row);

                ok.Click += (_, _) => DialogHost.CloseDialogCommand.Execute(new SettingsResult
                {
                    SN = snBox.Text,
                    ExtensionData = extBox.Text
                }, ok);
                cancel.Click += (_, _) => DialogHost.CloseDialogCommand.Execute(null, cancel);

                var result = await DialogHost.Show(content, "DialogHost");
                if (result is not SettingsResult settings) return;

                SetSN(copy, string.IsNullOrWhiteSpace(settings.SN) ? null : settings.SN);
                SetExtensionData(copy, string.IsNullOrWhiteSpace(settings.ExtensionData) ? null : settings.ExtensionData);
                MessageEvenTrigger?.Invoke($"{copy.GetType().Name}: {Loc("SN")}={GetSN(copy) ?? "-"} , {Loc("扩展数据")}={(GetExtensionData(copy)?.Length ?? 0)} 字符", copy);
            }
            catch (Exception ex)
            {
                // 应用未配置 DialogHost（Identifier = "DialogHost"）时仅提示，不中断
                MessageEvenTrigger?.Invoke($"{Loc("设置")}: {ex.Message}", copy);
            }
        }

        /// <summary>向设置面板追加 标签+输入框</summary>
        private static void AddField(StackPanel panel, string label, Control field)
        {
            panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 6, 0, 2) });
            panel.Children.Add(field);
        }

        /// <summary>提取：将副本在画布中的 X/Y 坐标复制到剪贴板</summary>
        /// <param name="copy">副本控件</param>
        public void ExtractCopy(FrameworkElement copy)
        {
            if (copy == null) return;
            double x = Canvas.GetLeft(copy);
            double y = Canvas.GetTop(copy);
            string text = $"X={(int)Math.Round(x)}, Y={(int)Math.Round(y)}";
            try
            {
                Clipboard.SetText(text);
                MessageEvenTrigger?.Invoke($"{copy.GetType().Name}: {Loc("提取")} {text}", copy);
            }
            catch (Exception ex)
            {
                MessageEvenTrigger?.Invoke($"{copy.GetType().Name}: {ex.Message}", copy);
            }
        }

        /// <summary>移除副本：先移除装饰器与移动/拖拽大小事件，再移出容器</summary>
        /// <param name="copy">副本控件</param>
        public void RemoveCopy(FrameworkElement copy)
        {
            if (copy == null) return;
            fixedCopies.Remove(copy);
            menuCopies.Remove(copy);
            dragControlsHelper.Remove(copy);
            if (LlayoutContainer is Canvas canvas)
            {
                canvas.Children.Remove(copy);
            }
            else if (LlayoutContainer is Grid grid)
            {
                grid.Children.Remove(copy);
            }
            MessageEvenTrigger?.Invoke($"{copy.GetType().Name}: {Loc("移除")}", copy);
        }

        /// <summary>设置副本旋转角度（度，绕中心、顺时针）。<br/>
        /// 无装饰器（如已固定）时仅旋转控件本身；有装饰器时周围点/手柄同步旋转。</summary>
        /// <param name="copy">副本控件</param>
        /// <param name="angle">角度（度）</param>
        public void SetRotation(FrameworkElement copy, double angle)
        {
            if (copy == null) return;
            var adorner = dragControlsHelper.Find(copy);
            if (adorner != null)
            {
                adorner.SetRotation(angle);
            }
            else
            {
                copy.RenderTransformOrigin = new Point(0.5, 0.5);
                copy.RenderTransform = new RotateTransform(angle % 360);
            }
        }

        /// <summary>读取副本旋转角度（度）；未设置过返回 0</summary>
        /// <param name="copy">副本控件</param>
        public double GetRotation(FrameworkElement copy)
        {
            if (copy == null) return 0;
            var adorner = dragControlsHelper.Find(copy);
            if (adorner != null) return adorner.Angle;
            return (copy.RenderTransform as RotateTransform)?.Angle ?? 0;
        }

        #endregion

        #region 方法
        /// <summary>
        /// 动态修改偏移量
        /// </summary>
        /// <param name="HeightOffset">高度偏移</param>
        /// <param name="WidthOffset">宽度偏移</param>
        public void DynamicUpdateOffset(double HeightOffset, double WidthOffset)
        {
            this.HeightOffset = HeightOffset;
            this.WidthOffset = WidthOffset;
        }

        /// <summary>
        /// 移动与拖拽大小添加
        /// </summary>
        /// <param name="Controls">控件</param>
        /// <param name="Window">窗体</param>
        /// <param name="Move">移动功能（含中心移动圈）</param>
        /// <param name="DragSize">拖拽大小功能（四周缩放点）</param>
        /// <param name="Rotate">旋转功能（顶部旋转圈）</param>
        public string MoveAndDragSizeInsert(FrameworkElement Controls, FrameworkElement Window, bool Move, bool DragSize, bool Rotate = false)
        {
            //创建拖动与拖拽大小
            return dragControlsHelper.Insert(Controls, Window, Move, DragSize, Rotate);
        }

        /// <summary>
        /// 移除拖拽大小与移动
        /// </summary>
        public void MoveAndDragSizeRemove(FrameworkElement Controls)
        {
            //创建拖动与拖拽大小
            dragControlsHelper.Remove(Controls);
        }
        /// <summary>
        ///  添加需要拖动的组件
        /// </summary>
        /// <param name="ControlsShow">界面上已经生成的控件</param>
        public void Insert(FrameworkElement ControlsShow)
        {
            if (!ShowControlsList.Contains(ControlsShow))  //不存在则添加
            {
                InsertEven(ControlsShow);
                ShowControlsList.Add(ControlsShow);
            }
        }
        /// <summary>
        /// 移除拖动
        /// </summary>
        /// <param name="ControlsShow">界面上已经生成的控件</param>
        public void Remove(FrameworkElement ControlsShow)
        {
            if (ShowControlsList.Contains(ControlsShow))
            {
                RemoveEven(ControlsShow);
                ShowControlsList.Remove(ControlsShow);  //直接移除
            }
        }
        /// <summary>
        /// 创建事件
        /// </summary>
        /// <param name="ControlsShow">界面上已经生成的控件</param>
        public void InsertEven(FrameworkElement ControlsShow)
        {
            //ControlsShow.PreviewMouseLeftButtonDown += delegate (object sender, MouseButtonEventArgs e) { ControlsShow_PreviewMouseLeftButtonDown(sender, e, ControlsObj); };
            //ControlsShow.PreviewMouseLeftButtonUp += delegate (object sender, MouseButtonEventArgs e) { ControlsShow_PreviewMouseLeftButtonUp(sender, e, ControlsObj); };
            //ControlsShow.PreviewMouseMove += delegate (object sender, MouseEventArgs e) { ControlsShow_PreviewMouseMove(sender, e, ControlsObj); };

            ControlsShow.PreviewMouseLeftButtonDown += ControlsShow_PreviewMouseLeftButtonDown;
            ControlsShow.PreviewMouseLeftButtonUp += ControlsShow_PreviewMouseLeftButtonUp;
            ControlsShow.PreviewMouseMove += ControlsShow_PreviewMouseMove;
            ControlsShow.LostMouseCapture += ControlsShow_LostMouseCapture;
        }
        /// <summary>
        /// 移除事件
        /// </summary>
        /// <param name="ControlsShow">界面上已经生成的控件</param>
        public void RemoveEven(FrameworkElement ControlsShow)
        {
            ControlsShow.PreviewMouseLeftButtonDown -= ControlsShow_PreviewMouseLeftButtonDown;
            ControlsShow.PreviewMouseLeftButtonUp -= ControlsShow_PreviewMouseLeftButtonUp;
            ControlsShow.PreviewMouseMove -= ControlsShow_PreviewMouseMove;
            ControlsShow.LostMouseCapture -= ControlsShow_LostMouseCapture;
        }

        #endregion

        #region 委托回调事件

        /// <summary>
        /// 定义委托 提醒拖拽事件开始了，请传需要拖动的按钮对象
        /// </summary>
        /// <param name="ShowControl">在哪个控件上触发了拖拽</param>
        /// <returns>返回已经创建了新的控件对象  -   是否需要移动   -  是否需要拖拽大小</returns>
        public delegate (FrameworkElement NewControl, bool IsMove, bool IsDragSize, bool IsRotate) dragEvenTrigger(FrameworkElement ShowControl);
        /// <summary>
        /// 实现委托
        /// </summary>
        public dragEvenTrigger DragEvenTrigger;

        /// <summary>
        /// 消息委托
        /// </summary>
        /// <param name="Message">消息</param>
        /// <param name="element">哪个控件显示的消息</param>
        public delegate void messageEvenTrigger(string Message, FrameworkElement element);
        /// <summary>
        /// 实现委托
        /// </summary>
        public messageEvenTrigger MessageEvenTrigger;
        #endregion

        #region 执行事件

        /// <summary>
        /// 取控件用于居中定位的实际尺寸：优先 ActualWidth/ActualHeight（已布局），
        /// 再退回声明的 Width/Height（可能为 NaN —— 部分自定义控件遮蔽了 FrameworkElement.Height），
        /// 最后退回 0，保证定位计算不会产生 NaN。
        /// </summary>
        private static (double W, double H) GetDragSize(FrameworkElement el)
        {
            double w = el.ActualWidth > 0 ? el.ActualWidth : el.Width;
            double h = el.ActualHeight > 0 ? el.ActualHeight : el.Height;
            return (double.IsNaN(w) ? 0 : w, double.IsNaN(h) ? 0 : h);
        }

        /// <summary>
        /// 源控件自检（保险丝）：拖拽期间装饰器层会触发全局布局，个别控件在全局重排后
        /// 可能被压成 0 尺寸（如内部内容测量塌缩）。这里对已注册的拖动源强制恢复可见并重测，
        /// 保证“左侧源”在任何拖拽流程后都原样存在、不被影响。
        /// </summary>
        private void EnsureSourcesVisible()
        {
            foreach (var source in ShowControlsList)
            {
                if (source == null || source.IsVisible) continue;
                source.Visibility = Visibility.Visible;
                source.InvalidateMeasure();
                source.InvalidateVisual();
            }
        }

        /// <summary>
        /// 判断鼠标（窗体坐标）是否位于拖拽容器（画布）范围内。<br/>
        /// 容器在窗体中的偏移通过 TranslatePoint 实时获取，兼容窗体标题栏/边框差异。
        /// </summary>
        private bool IsInContainer(Point posInWindow)
        {
            if (LlayoutContainer is not FrameworkElement container) return true;
            var origin = container.TranslatePoint(new Point(0, 0), Windows);
            var rect = new Rect(origin, new Size(container.ActualWidth, container.ActualHeight));
            return rect.Contains(posInWindow);
        }

        /// <summary>
        /// 按鼠标位置刷新副本位置与可见性。<br/>
        /// 副本只在拖拽容器（画布）内显示并跟随鼠标 —— 鼠标在容器外（如左侧源面板上方）时
        /// 副本折叠隐藏，避免副本/装饰器覆盖在拖动源上造成“源不见了/源被挪走”的错觉。
        /// </summary>
        private void UpdateDragClone(Point posInWindow)
        {
            var (w, h) = GetDragSize(ControlsObj);
            double left = (posInWindow.X - w / 2) - WidthOffset;
            double top = (posInWindow.Y - h / 2) - HeightOffset;
            if (LlayoutContainer is Canvas canvas)
            {
                Canvas.SetLeft(ControlsObj, left);
                Canvas.SetTop(ControlsObj, top);
            }
            else if (LlayoutContainer is Grid grid)
            {
                ControlsObj.Margin = new Thickness(left, top, Windows.ActualWidth - left - w, Windows.ActualHeight - top - h);
            }
            ControlsObj.Visibility = IsInContainer(posInWindow) ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 鼠标移动事件处理。<br/>
        /// 根据容器类型（Canvas 或 Grid）计算并更新控件位置。
        /// </summary>
        private void ControlsShow_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (ControlsObj == null) return;
            if (IsMouseDown)
            {
                UpdateDragClone(e.GetPosition(Windows));
            }
        }

        /// <summary>
        /// 取消拖拽：移除画布/网格容器中的副本及其装饰器（鼠标在容器外松开或捕获丢失时调用）。
        /// </summary>
        private void CancelDragClone()
        {
            if (ControlsObj == null) return;
            dragControlsHelper.Remove(ControlsObj);
            menuCopies.Remove(ControlsObj);
            if (LlayoutContainer is Canvas canvas)
            {
                canvas.Children.Remove(ControlsObj);
            }
            else if (LlayoutContainer is Grid grid)
            {
                grid.Children.Remove(ControlsObj);
            }
            ControlsObj = null;
            EnsureSourcesVisible();
        }

        /// <summary>
        /// 鼠标左键松开事件处理。<br/>
        /// 停止拖动并恢复控件透明度，释放鼠标捕获。<br/>
        /// 在容器（画布）外松开视为取消：丢弃副本，源面板不受任何影响。
        /// </summary>
        private void ControlsShow_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            IsMouseDown = false;
            if (sender is UIElement uiElement)
            {
                uiElement.ReleaseMouseCapture();
            }
            if (ControlsObj == null) return;
            if (!IsInContainer(e.GetPosition(Windows)))
            {
                // 画布外松开 = 取消拖拽，移除副本（源面板保持原样）
                CancelDragClone();
                return;
            }
            ControlsObj.Visibility = Visibility.Visible;
            ControlsObj.Opacity = 1;
            ControlsObj = null;
            EnsureSourcesVisible();
        }

        /// <summary>
        /// 鼠标捕获丢失事件处理。<br/>
        /// 捕获被其他窗口抢占（如 Alt+Tab 切换）时，恢复拖动状态，避免副本卡在半透明状态。
        /// </summary>
        private void ControlsShow_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (!IsMouseDown) return;
            IsMouseDown = false;
            if (ControlsObj == null) return;
            if (!IsInContainer(Mouse.GetPosition(Windows)))
            {
                // 捕获丢失且鼠标在画布外：丢弃副本
                CancelDragClone();
                return;
            }
            ControlsObj.Visibility = Visibility.Visible;
            ControlsObj.Opacity = 1;
            ControlsObj = null;
        }


        /// <summary>
        /// 鼠标左键按下事件处理。<br/>
        /// 通过委托创建新控件，设置半透明并添加到容器中，启动拖动。
        /// </summary>
        private void ControlsShow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (LlayoutContainer.GetType().Equals(typeof(Canvas)))
            {
                Canvas layout = LlayoutContainer as Canvas;
                (FrameworkElement element, bool IsMove, bool IsDragSize, bool IsRotate) = DragEvenTrigger(sender as FrameworkElement);
                ControlsObj = element;
                if (!layout.Children.Contains(ControlsObj))
                {
                    IsMouseDown = true;
                    // 防御：触发器若返回源对象本身，强制按类型克隆，绝不把拖动源搬进画布
                    if (ReferenceEquals(ControlsObj, sender))
                    {
                        ControlsObj = DefaultClone(sender as FrameworkElement);
                    }
                    // 捕获鼠标：拖出源控件边界后继续跟随，直到左键松开
                    if (sender is UIElement source)
                    {
                        source.CaptureMouse();
                        // 关键：终止后续路由 —— 源控件内部（如 TextBox/TextBoxControl）若再捕获鼠标，
                        // 会覆盖源控件的捕获，导致 PreviewMouseMove 收不到、副本不跟随鼠标
                        e.Handled = true;
                    }
                    Point Position = e.GetPosition(Windows);
                    ControlsObj.Opacity = 0.5;
                    // 先加入容器并强制布局，再用实际尺寸定位 ——
                    // 自定义控件可能遮蔽 FrameworkElement.Width/Height（值为 NaN），
                    // 直接用 Width/Height 计算会产生 NaN 坐标导致副本钉在容器左上角
                    layout.Children.Add(ControlsObj);
                    ControlsObj.UpdateLayout();
                    UpdateDragClone(Position);
                    AttachCopyMenu(ControlsObj);
                    // 副本携带源名称（布局持久化 SourceName）
                    if (sender is FrameworkElement sourceElement && GetSourceName(sourceElement) is { } sourceName)
                    {
                        SetSourceName(ControlsObj, sourceName);
                    }
                    //添加拖拽大小与移动
                    MessageEvenTrigger(MoveAndDragSizeInsert(ControlsObj, Windows, IsMove, IsDragSize, IsRotate), sender as FrameworkElement);
                    // 保险丝：装饰器层全局布局后，确保源控件未被压成 0 高（0 尺寸时强制恢复可见）
                    EnsureSourcesVisible();
                }
                else
                {
                    MessageEvenTrigger("此控件已在布局中存在", sender as FrameworkElement);
                    ControlsObj = null;
                }
            }
            else if (LlayoutContainer.GetType().Equals(typeof(Grid)))
            {
                Grid layout = LlayoutContainer as Grid;
                (FrameworkElement element, bool IsMove, bool IsDragSize, bool IsRotate) = DragEvenTrigger(sender as FrameworkElement);
                ControlsObj = element;
                if (!layout.Children.Contains(ControlsObj))
                {
                    IsMouseDown = true;
                    // 防御：触发器若返回源对象本身，强制按类型克隆，绝不把拖动源搬进画布
                    if (ReferenceEquals(ControlsObj, sender))
                    {
                        ControlsObj = DefaultClone(sender as FrameworkElement);
                    }
                    // 捕获鼠标：拖出源控件边界后继续跟随，直到左键松开
                    if (sender is UIElement source)
                    {
                        source.CaptureMouse();
                        // 关键：终止后续路由 —— 源控件内部（如 TextBox/TextBoxControl）若再捕获鼠标，
                        // 会覆盖源控件的捕获，导致 PreviewMouseMove 收不到、副本不跟随鼠标
                        e.Handled = true;
                    }
                    Point Position = e.GetPosition(Windows);
                    ControlsObj.Opacity = 0.5;

                    // 先加入容器并强制布局，再用实际尺寸定位（防 NaN 坐标）
                    layout.Children.Add(ControlsObj);
                    ControlsObj.UpdateLayout();
                    UpdateDragClone(Position);

                    AttachCopyMenu(ControlsObj);
                    // 副本携带源名称（布局持久化 SourceName）
                    if (sender is FrameworkElement sourceElement && GetSourceName(sourceElement) is { } sourceName)
                    {
                        SetSourceName(ControlsObj, sourceName);
                    }
                    //添加拖拽大小与移动
                    MessageEvenTrigger(MoveAndDragSizeInsert(ControlsObj, Windows, IsMove, IsDragSize, IsRotate), sender as FrameworkElement);
                    // 保险丝：装饰器层全局布局后，确保源控件未被压成 0 高（0 尺寸时强制恢复可见）
                    EnsureSourcesVisible();
                }
                else
                {
                    MessageEvenTrigger("此控件已在布局中存在", sender as FrameworkElement);
                    ControlsObj = null;
                }
            }
        }


        /// <summary>
        /// 窗体大小变化事件处理。<br/>
        /// 同步更新布局容器的大小以匹配窗体大小。
        /// </summary>
        private void Windwos_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FrameworkElement window = sender as FrameworkElement;
            if (LlayoutContainer.GetType().Equals(typeof(Canvas)))
            {
                Canvas layout = LlayoutContainer as Canvas;
                layout.Width = window.ActualWidth;
                layout.Height = window.ActualHeight;
            }
            else if (LlayoutContainer.GetType().Equals(typeof(Grid)))
            {
                Grid layout = LlayoutContainer as Grid;
                layout.Width = window.ActualWidth;
                layout.Height = window.ActualHeight;
            }
        }
        #endregion

    }
}
