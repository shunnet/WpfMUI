using Microsoft.VisualStudio.TestTools.UnitTesting;
using Snet.Windows.Controls.property.wpf;
using Snet.Windows.Controls.property.wpf.Operators;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Snet.Windows.Tests;

/// <summary>
/// property 模块（PropertyTools 移植）审查（2026-11）发现的缺陷的回归测试。
/// </summary>
[TestClass]
public sealed class PropertyRegressionTests
{
    public sealed class LabelModel
    {
        public string Name { get; set; } = string.Empty;

        public int Mode { get; set; }
    }

    private static FrameworkElement CreateLabel(PropertyItem item)
    {
        var grid = new PropertyGrid();
        MethodInfo createLabel = typeof(PropertyGrid).GetMethod("CreateLabel", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (FrameworkElement)createLabel.Invoke(grid, [item])!;
    }

    [STATestMethod]
    public void PropertyGrid_RadioButtonLabel_ShowsThePropertyDisplayName()
    {
        PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(LabelModel));
        var item = new PropertyItem(props[nameof(LabelModel.Name)]!, props)
        {
            RadioDescriptor = props[nameof(LabelModel.Mode)],
            RadioValue = 1
        };

        item.DisplayName = "Mode label";

        var label = (Grid)CreateLabel(item);
        var radio = label.Children.OfType<RadioButton>().Single();

        // 修复前：RadioButton 没有 Content，标签位置只剩一个没有文字的单选按钮
        Assert.AreEqual("Mode label", radio.Content);

        // 语言切换会更新 PropertyItem.DisplayName，标签必须随之刷新（绑定，而不是一次性赋值）
        item.DisplayName = "Changed";
        Assert.AreEqual("Changed", radio.Content);
    }

    [STATestMethod]
    public void PropertyGrid_OptionalPropertyCheckBoxLabel_FollowsTheDisplayName()
    {
        PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(LabelModel));
        var item = new PropertyItem(props[nameof(LabelModel.Name)]!, props) { IsOptional = true };
        item.DisplayName = "Optional label";

        var label = (Grid)CreateLabel(item);
        var checkBox = label.Children.OfType<CheckBox>().Single();
        Assert.AreEqual("Optional label", checkBox.Content);

        // 修复前：复选框标签是一次性赋值，语言切换更新 DisplayName 后仍显示旧文字
        item.DisplayName = "Changed";
        Assert.AreEqual("Changed", checkBox.Content);
    }

    [STATestMethod]
    public void PropertyGrid_PlainLabel_DoesNotReplaceTheDataContext()
    {
        PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(LabelModel));
        var item = new PropertyItem(props[nameof(LabelModel.Name)]!, props) { DisplayName = "Name label" };

        var label = (Label)CreateLabel(item);

        // 修复前：标签的 DataContext 被设成 PropertyItem，标签上基于被编辑对象的绑定（IsEnabled 约定等）会解析到 PropertyItem 而失效
        Assert.AreEqual(DependencyProperty.UnsetValue, label.ReadLocalValue(FrameworkElement.DataContextProperty));
        Assert.AreEqual("Name label", label.Content);
    }

    [TestMethod]
    public void FormattedTimeSpanParser_NonMatchingInput_ThrowsInsteadOfReturningZero()
    {
        var parser = new FormattedTimeSpanParser("hh:mm:ss");

        Assert.AreEqual(new TimeSpan(1, 2, 3), parser.Parse("01:02:03"));

        // 修复前：不匹配的输入被静默解析成 TimeSpan.Zero，用户的错误输入把值悄悄改成 00:00:00
        Assert.ThrowsExactly<FormatException>(() => parser.Parse("not a time"));
        Assert.ThrowsExactly<FormatException>(() => parser.Parse(string.Empty));
    }

    [TestMethod]
    public void FormattedTimeSpanParser_TryParse_ReportsFailureWithoutThrowing()
    {
        var parser = new FormattedTimeSpanParser("hh:mm:ss");

        Assert.IsTrue(parser.TryParse("01:02:03", out TimeSpan ok));
        Assert.AreEqual(new TimeSpan(1, 2, 3), ok);
        Assert.IsFalse(parser.TryParse("garbage", out TimeSpan bad));
        Assert.AreEqual(TimeSpan.Zero, bad);
        Assert.IsFalse(parser.TryParse(string.Empty, out _));
    }

    [TestMethod]
    public void FormattedTimeSpanParser_PartialMatches_AreRejectedInsteadOfSilentlyMisparsed()
    {
        var parser = new FormattedTimeSpanParser("hh:mm:ss");

        // 修复前：正则没有锚定，只要字符串里"含有"一段匹配就接受：123:45:67 被读成 23:45:67（再折算成 23:46:07），尾随的垃圾被丢弃
        Assert.IsFalse(parser.TryParse("123:45:67", out _));
        Assert.IsFalse(parser.TryParse("x01:02:03abc", out _));
        Assert.ThrowsExactly<FormatException>(() => parser.Parse("100:00:00"));

        // 首尾空白仍然允许（用户在文本框里多敲了空格）
        Assert.AreEqual(new TimeSpan(1, 2, 3), parser.Parse("  01:02:03 "));
    }

    #region B-03 模板重新应用

    public sealed class TabsModel
    {
        [Category("First|Group")]
        public string A { get; set; } = string.Empty;

        [Category("Second|Group")]
        public string B { get; set; } = string.Empty;
    }

    private static ControlTemplate CreateGridTemplate(bool bindSelectedIndex = false)
    {
        var root = new FrameworkElementFactory(typeof(Grid));
        var tabs = new FrameworkElementFactory(typeof(TabControl)) { Name = "PART_Tabs" };
        if (bindSelectedIndex)
        {
            // 与默认模板一致：TabControl.SelectedIndex 与 PropertyGrid.SelectedTabIndex 双向绑定
            tabs.SetBinding(
                System.Windows.Controls.Primitives.Selector.SelectedIndexProperty,
                new Binding(nameof(PropertyGrid.SelectedTabIndex)) { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        }

        var scroll = new FrameworkElementFactory(typeof(ScrollViewer)) { Name = "PART_ScrollViewer" };
        var panel = new FrameworkElementFactory(typeof(StackPanel)) { Name = "PART_Panel" };
        scroll.AppendChild(panel);
        root.AppendChild(tabs);
        root.AppendChild(scroll);
        return new ControlTemplate(typeof(PropertyGrid)) { VisualTree = root };
    }

    [STATestMethod]
    public void PropertyGrid_ReappliedTemplate_RebuildsTheControls()
    {
        var grid = new PropertyGrid { Template = CreateGridTemplate(), SelectedObject = new LabelModel { Name = "x" } };
        grid.ApplyTemplate();
        var firstTabs = (TabControl)grid.Template.FindName("PART_Tabs", grid);
        Assert.IsGreaterThan(0, firstTabs.Items.Count, "初次应用模板后应生成属性内容");

        // 同一个 PropertyGrid、同一个 SelectedObject，只是模板被替换（例如主题切换导致模板重新加载）
        grid.Template = CreateGridTemplate();
        grid.ApplyTemplate();
        var secondTabs = (TabControl)grid.Template.FindName("PART_Tabs", grid);

        Assert.AreNotSame(firstTabs, secondTabs);

        // 修复前：UpdateControls 认为"对象与外观戳都没变"而直接返回，新模板里什么都没有
        Assert.IsGreaterThan(0, secondTabs.Items.Count, "重新应用模板后新部件里应重新生成属性内容");
    }

    [STATestMethod]
    public void PropertyGrid_ReappliedTemplate_KeepsTheSelectedTab()
    {
        var grid = new PropertyGrid { Template = CreateGridTemplate(bindSelectedIndex: true), SelectedObject = new TabsModel() };
        grid.ApplyTemplate();
        var firstTabs = (TabControl)grid.Template.FindName("PART_Tabs", grid);
        Assert.AreEqual(2, firstTabs.Items.Count);

        firstTabs.SelectedIndex = 1;
        Assert.AreEqual(1, grid.SelectedTabIndex, "双向绑定把选中的页写回 SelectedTabIndex");

        // 换肤等原因使模板重新应用：新模板的 TabControl 是空的，它的 SelectedIndex 绑定会先把 SelectedTabIndex 清成 -1
        grid.Template = CreateGridTemplate(bindSelectedIndex: true);
        grid.ApplyTemplate();
        var secondTabs = (TabControl)grid.Template.FindName("PART_Tabs", grid);

        Assert.AreNotSame(firstTabs, secondTabs);
        Assert.AreEqual(2, secondTabs.Items.Count);

        // 修复前：用户选中的页丢失（回到第一页，SelectedTabIndex 被覆盖）
        Assert.AreEqual(1, secondTabs.SelectedIndex);
        Assert.AreEqual(1, grid.SelectedTabIndex);
    }

    #endregion

    #region B-04 实例级描述符不能按类型缓存

    [STATestMethod]
    public void PropertyGridOperator_DataRowViewsOfDifferentTables_GetTheirOwnProperties()
    {
        var first = new DataTable();
        first.Columns.Add("Alpha", typeof(string));
        first.Rows.Add("a");

        var second = new DataTable();
        second.Columns.Add("Beta", typeof(int));
        second.Rows.Add(1);

        var grid = new PropertyGrid();
        var op = new PropertyGridOperator();

        string[] PropertyNames(DataRowView row) => op.CreateModel(row, false, grid)!
            .SelectMany(t => t.Groups)
            .SelectMany(g => g.Properties)
            .Select(p => p.PropertyName)
            .ToArray();

        CollectionAssert.AreEqual(new[] { "Alpha" }, PropertyNames(first.DefaultView[0]));

        // 修复前：DataRowView 是同一个 CLR 类型，第二个实例沿用了第一个实例缓存的属性列表（仍是 Alpha）
        CollectionAssert.AreEqual(new[] { "Beta" }, PropertyNames(second.DefaultView[0]));
    }

    public sealed class ProviderModel
    {
        public int Real { get; set; }
    }

    private sealed class ExtraPropertyDescriptor : PropertyDescriptor
    {
        public ExtraPropertyDescriptor(string name)
            : base(name, null)
        {
        }

        public override Type ComponentType => typeof(ProviderModel);

        public override bool IsReadOnly => false;

        public override Type PropertyType => typeof(string);

        public override bool CanResetValue(object component) => false;

        public override object? GetValue(object? component) => null;

        public override void ResetValue(object component)
        {
        }

        public override void SetValue(object? component, object? value)
        {
        }

        public override bool ShouldSerializeValue(object component) => false;
    }

    private sealed class ExtraDescriptor : CustomTypeDescriptor
    {
        private readonly string extraName;

        public ExtraDescriptor(ICustomTypeDescriptor? parent, string extraName)
            : base(parent)
        {
            this.extraName = extraName;
        }

        public override PropertyDescriptorCollection GetProperties() => this.Extend(base.GetProperties());

        public override PropertyDescriptorCollection GetProperties(Attribute[]? attributes) => this.Extend(base.GetProperties(attributes));

        private PropertyDescriptorCollection Extend(PropertyDescriptorCollection inner)
        {
            var all = inner.Cast<PropertyDescriptor>().ToList();
            all.Add(new ExtraPropertyDescriptor(this.extraName));
            return new PropertyDescriptorCollection(all.ToArray(), true);
        }
    }

    private sealed class InstanceProvider : TypeDescriptionProvider
    {
        private readonly string extraName;

        public InstanceProvider(TypeDescriptionProvider parent, string extraName)
            : base(parent)
        {
            this.extraName = extraName;
        }

        public override ICustomTypeDescriptor? GetTypeDescriptor(Type objectType, object? instance) =>
            new ExtraDescriptor(base.GetTypeDescriptor(objectType, instance), this.extraName);
    }

    [STATestMethod]
    public void PropertyGridOperator_InstanceLevelTypeDescriptionProviders_GetTheirOwnProperties()
    {
        var first = new ProviderModel();
        var second = new ProviderModel();
        TypeDescriptor.AddProvider(new InstanceProvider(TypeDescriptor.GetProvider(first), "ExtraFirst"), first);
        TypeDescriptor.AddProvider(new InstanceProvider(TypeDescriptor.GetProvider(second), "ExtraSecond"), second);

        var grid = new PropertyGrid();
        var op = new PropertyGridOperator();

        string[] PropertyNames(object instance) => op.CreateModel(instance, false, grid)!
            .SelectMany(t => t.Groups)
            .SelectMany(g => g.Properties)
            .Select(p => p.PropertyName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(new[] { "ExtraFirst", "Real" }, PropertyNames(first));

        // 修复前：这类对象没有实现 ICustomTypeDescriptor，缓存只按 CLR 类型区分，第二个实例沿用了第一个实例的属性列表
        CollectionAssert.AreEqual(new[] { "ExtraSecond", "Real" }, PropertyNames(second));
    }

    [STATestMethod]
    public void PropertyGridOperator_PlainObjectsOfTheSameType_ReuseTheCachedBrowsableList()
    {
        var op = new PropertyGridOperator();
        MethodInfo cached = typeof(PropertyGridOperator).GetMethod("GetBrowsablePropertiesCached", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var a = new LabelModel();
        var b = new LabelModel();

        object? listA = cached.Invoke(op, [a, TypeDescriptor.GetProperties(a)]);
        object? listB = cached.Invoke(op, [b, TypeDescriptor.GetProperties(b)]);

        // 缓存的意义：普通对象每次重建属性网格时不必重新做属性过滤（这条用例防止缓存校验过严而永远不命中）
        Assert.AreSame(listA, listB);
    }

    #endregion

    #region B-05 描述本地化钩子

    private sealed class RecordingLocalizer : ILocalizableOperator
    {
        public string GetLocalizedString(string key, Type declaringType) => "str:" + key;

        public string GetLocalizedDescription(string key, Type declaringType) => "desc:" + key;
    }

    [STATestMethod]
    public void PropertyGridOperator_EnglishDescription_IsLocalizedThroughGetLocalizedDescription()
    {
        PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(LabelModel));
        var item = new PropertyItem(props[nameof(LabelModel.Name)]!, props);
        var op = new PropertyGridOperator();
        op.UseLocalizableOperator(new RecordingLocalizer());

        MethodInfo setLang = typeof(PropertyGridOperator).GetMethod("SetLang", BindingFlags.Instance | BindingFlags.NonPublic)!;
        setLang.Invoke(op, [item, typeof(LabelModel), "Name", "The name", Snet.Model.@enum.LanguageType.en]);

        Assert.AreEqual("str:Name", item.DisplayName);

        // 修复前：描述也走了 GetLocalizedString，自定义实现里的 GetLocalizedDescription 永远不会被调用
        Assert.AreEqual("desc:The name", item.Description);
    }

    private sealed class StringOnlyOperator : PropertyGridOperator
    {
        public override string GetLocalizedString(string key, Type declaringType) => "str:" + key;
    }

    [STATestMethod]
    public void PropertyGridOperator_SubclassOverridingOnlyGetLocalizedString_StillLocalizesDescriptions()
    {
        PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(LabelModel));
        var item = new PropertyItem(props[nameof(LabelModel.Name)]!, props);
        var op = new StringOnlyOperator();

        MethodInfo setLang = typeof(PropertyGridOperator).GetMethod("SetLang", BindingFlags.Instance | BindingFlags.NonPublic)!;
        setLang.Invoke(op, [item, typeof(LabelModel), "Name", "The name", Snet.Model.@enum.LanguageType.en]);

        Assert.AreEqual("str:Name", item.DisplayName);

        // 兼容：描述改走 GetLocalizedDescription 之后，只重写了 GetLocalizedString 的子类（原先描述就是经由它本地化的）不能丢掉描述的本地化
        Assert.AreEqual("str:The name", item.Description);
    }

    #endregion

    #region B-06 / B-07 INotifyDataErrorInfo 错误提示

    public sealed class TwoErrorModel : INotifyDataErrorInfo
    {
        private readonly Dictionary<string, string> errors = new();

        public string A { get; set; } = string.Empty;

        public string B { get; set; } = string.Empty;

        public bool HasErrors => this.errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName) =>
            propertyName != null && this.errors.TryGetValue(propertyName, out string? message) ? new[] { message } : Array.Empty<string>();

        public void SetError(string propertyName, string? message)
        {
            if (message == null)
            {
                this.errors.Remove(propertyName);
            }
            else
            {
                this.errors[propertyName] = message;
            }

            this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
    }

    private static (Tab Tab, ContentControl ErrorA, ContentControl ErrorB) CreateErrorRows(TwoErrorModel model)
    {
        PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(TwoErrorModel));
        var itemA = new PropertyItem(props[nameof(TwoErrorModel.A)]!, props);
        var itemB = new PropertyItem(props[nameof(TwoErrorModel.B)]!, props);

        var group = new Group();
        group.Properties.Add(itemA);
        group.Properties.Add(itemB);
        var tab = new Tab();
        tab.Groups.Add(group);

        var factory = new PropertyGridControlFactory();
        var options = new PropertyControlFactoryOptions();
        return (tab, factory.CreateErrorControl(itemA, model, tab, options), factory.CreateErrorControl(itemB, model, tab, options));
    }

    private static void PumpDispatcher()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    [STATestMethod]
    public void ErrorControls_TwoFailingPropertiesInOneTab_EachShowItsOwnCurrentError()
    {
        var model = new TwoErrorModel();
        var (_, errorA, errorB) = CreateErrorRows(model);

        Assert.AreEqual(Visibility.Collapsed, errorA.Visibility);
        Assert.AreEqual(Visibility.Collapsed, errorB.Visibility);

        // A 出错：只有 A 的提示可见（修复前：B 也因为对象级 HasErrors 而可见，内容为空）
        model.SetError("A", "A is bad");
        Assert.AreEqual(Visibility.Visible, errorA.Visibility);
        Assert.AreEqual("A is bad", errorA.Content);
        Assert.AreEqual(Visibility.Collapsed, errorB.Visibility);

        // B 也出错：Tab.HasErrors 已经是 true，不会再翻转。修复前 B 的提示不会出现
        model.SetError("B", "B is bad");
        Assert.AreEqual(Visibility.Visible, errorB.Visibility);
        Assert.AreEqual("B is bad", errorB.Content);
        Assert.AreEqual("A is bad", errorA.Content);

        // A 改好：B 仍然出错，Tab.HasErrors 仍是 true。修复前 A 的旧提示会继续显示
        model.SetError("A", null);
        Assert.AreEqual(Visibility.Collapsed, errorA.Visibility);
        Assert.IsNull(errorA.Content);
        Assert.AreEqual(Visibility.Visible, errorB.Visibility);
        Assert.AreEqual("B is bad", errorB.Content);

        model.SetError("B", null);
        Assert.AreEqual(Visibility.Collapsed, errorB.Visibility);
        Assert.IsNull(errorB.Content);
    }

    [STATestMethod]
    public void ErrorsChanged_RaisedOnAWorkerThread_IsHandledOnTheUiThread()
    {
        var model = new TwoErrorModel();
        var (tab, errorA, _) = CreateErrorRows(model);

        int uiThread = Environment.CurrentManagedThreadId;
        var notifiedOnThreads = new List<int>();
        tab.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Tab.HasErrors))
            {
                notifiedOnThreads.Add(Environment.CurrentManagedThreadId);
            }
        };

        // 异步校验常在线程池线程上触发 ErrorsChanged。登记表和错误控件只能在 UI 线程上访问
        Task.Run(() => model.SetError("A", "A is bad")).Wait();
        PumpDispatcher();

        CollectionAssert.AreEqual(new[] { uiThread }, notifiedOnThreads);
        Assert.AreEqual("A is bad", errorA.Content);
    }

    /// <summary>带自己的 Dispatcher 消息循环的 STA 线程；调度器里的未处理异常被记录下来（而不是让测试进程崩溃）。</summary>
    private sealed class UiThread : IDisposable
    {
        private readonly Thread thread;

        public UiThread(string name)
        {
            Dispatcher? dispatcher = null;
            using var ready = new ManualResetEventSlim();
            this.thread = new Thread(() =>
            {
                dispatcher = Dispatcher.CurrentDispatcher;
                dispatcher.UnhandledException += (_, e) =>
                {
                    this.Unhandled = e.Exception;
                    e.Handled = true;
                };
                ready.Set();
                Dispatcher.Run();
            })
            {
                IsBackground = true,
                Name = name
            };
            this.thread.SetApartmentState(ApartmentState.STA);
            this.thread.Start();
            ready.Wait();
            this.Dispatcher = dispatcher!;
        }

        public Dispatcher Dispatcher { get; }

        public Exception? Unhandled { get; private set; }

        public T Invoke<T>(Func<T> action) => this.Dispatcher.Invoke(action);

        /// <summary>等到队列里已有的 Normal 优先级工作都处理完。</summary>
        public void Flush() => this.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);

        public void Stop()
        {
            if (this.thread.IsAlive)
            {
                this.Dispatcher.InvokeShutdown();
                this.thread.Join();
            }
        }

        public void Dispose() => this.Stop();
    }

    [STATestMethod]
    public void ErrorsChanged_ModelShownOnTwoUiThreads_EachRowIsUpdatedOnItsOwnThread()
    {
        var model = new TwoErrorModel();
        using var other = new UiThread("second-ui");
        var otherRows = other.Invoke(() => CreateErrorRows(model));
        var (_, mainErrorA, _) = CreateErrorRows(model);

        Task.Run(() => model.SetError("A", "A is bad")).Wait();
        PumpDispatcher();
        other.Flush();

        // 修复前：只记录了"第一个登记者"的调度器，所有行都在那个线程上刷新，访问另一个线程拥有的控件抛 InvalidOperationException
        Assert.IsNull(other.Unhandled, other.Unhandled?.Message);
        Assert.AreEqual("A is bad", mainErrorA.Content);
        Assert.AreEqual("A is bad", other.Invoke(() => otherRows.ErrorA.Content));
    }

    [STATestMethod]
    public void ErrorsChanged_FirstRegistrantsDispatcherIsGone_LaterRowsAreStillUpdated()
    {
        var model = new TwoErrorModel();
        using (var shortLived = new UiThread("short-lived-ui"))
        {
            // 先在一个短命的 STA 线程上显示过这个模型（例如离屏渲染/打印），然后线程与调度器都结束了
            shortLived.Invoke(() => CreateErrorRows(model));
            shortLived.Stop();
        }

        var (_, errorA, _) = CreateErrorRows(model);
        Task.Run(() => model.SetError("A", "A is bad")).Wait();
        PumpDispatcher();

        // 修复前：订阅只记录第一个登记者的调度器，后来在主线程上显示的行的更新被转发给已经结束的调度器，永远不会刷新
        Assert.AreEqual("A is bad", errorA.Content);
    }

    [STATestMethod]
    public void UpdateTabForValidationResults_OnlyFlagsTheTabThatContainsTheFailingProperty()
    {
        var model = new TwoErrorModel();
        var (tab, _, _) = CreateErrorRows(model);

        // 对象上有一个不属于本 Tab 的属性出错：本 Tab 不应显示错误标记
        model.SetError("SomethingElse", "not on this tab");
        new PropertyGridControlFactory().UpdateTabForValidationResults(tab, model);
        Assert.IsFalse(tab.HasErrors);

        model.SetError("B", "B is bad");
        new PropertyGridControlFactory().UpdateTabForValidationResults(tab, model);
        Assert.IsTrue(tab.HasErrors);
    }

    #endregion

    #region B-09 描述提示

    private static DockPanel AddLabelAndGetLabelPanel(PropertyGrid grid, PropertyItem item)
    {
        var propertyPanel = new Grid();
        MethodInfo addLabel = typeof(PropertyGrid).GetMethod("AddLabel", BindingFlags.Instance | BindingFlags.NonPublic)!;
        addLabel.Invoke(grid, [item, propertyPanel, new Label(), new TextBox(), null]);
        return propertyPanel.Children.OfType<DockPanel>().Single();
    }

    private static PropertyItem CreateItemWithDescription(string description)
    {
        PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(LabelModel));
        return new PropertyItem(props[nameof(LabelModel.Name)]!, props) { DisplayName = "Name", Description = description };
    }

    [STATestMethod]
    public void PropertyGrid_DescriptionToolTip_WithoutIcon_FollowsTheDescriptionAndTheToolTipTemplate()
    {
        var grid = new PropertyGrid();
        PropertyItem item = CreateItemWithDescription("first");

        DockPanel labelPanel = AddLabelAndGetLabelPanel(grid, item);
        Assert.AreEqual("first", labelPanel.ToolTip);

        // 语言切换会更新 Description，提示必须随之刷新
        item.Description = "second";
        Assert.AreEqual("second", labelPanel.ToolTip);

        // 修复前：这个分支用裸绑定绕过了 CreateToolTip，ToolTipTemplate 对它无效
        grid.ToolTipTemplate = new DataTemplate();
        DockPanel templated = AddLabelAndGetLabelPanel(grid, item);
        var content = Assert.IsInstanceOfType<ContentControl>(templated.ToolTip);
        Assert.AreEqual("second", content.Content);
        Assert.AreSame(grid.ToolTipTemplate, content.ContentTemplate);
    }

    [STATestMethod]
    public void PropertyGrid_DescriptionToolTip_WithIcon_FollowsTheDescription()
    {
        var grid = new PropertyGrid
        {
            ShowDescriptionIcons = true,
            DescriptionIcon = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Gray8, null, new byte[] { 0 }, 1)
        };
        PropertyItem item = CreateItemWithDescription("first");

        DockPanel labelPanel = AddLabelAndGetLabelPanel(grid, item);
        Image icon = labelPanel.Children.OfType<Image>().Single();
        Assert.AreEqual("first", icon.ToolTip);

        // 修复前：图标的提示是一次性赋值，语言切换后仍显示旧文字
        item.Description = "second";
        Assert.AreEqual("second", icon.ToolTip);
    }

    #endregion

    #region CTRL-M02 DataGrid 鼠标按下

    [TestMethod]
    public void DataGrid_MouseLeftButtonDown_WithoutPresentationSource_DoesNotThrow()
    {
        try
        {
            WpfTestHost.RunVoid(() =>
            {
                // DataGrid 的默认样式由 PropertyControl.xaml 通过 pack://application 合并进应用资源，需要 Application
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/Snet.Windows.Controls;component/property/wpf/DataGrid/Generic.xaml")
                });

                var grid = new Snet.Windows.Controls.property.wpf.DataGrid();

                // 没放进任何树的控件不会自动解析隐式样式，显式指定后才有模板部件（sheetGrid 等）
                grid.Style = (Style)Application.Current.Resources[typeof(Snet.Windows.Controls.property.wpf.DataGrid)];
                grid.ApplyTemplate();

                var args = new System.Windows.Input.MouseButtonEventArgs(
                    System.Windows.Input.Mouse.PrimaryDevice, Environment.TickCount, System.Windows.Input.MouseButton.Left)
                {
                    RoutedEvent = UIElement.MouseLeftButtonDownEvent,
                    Source = grid
                };

                // 修复前：控件没有连接到 PresentationSource（没放进任何窗口）时 PointToScreen 抛出 InvalidOperationException
                grid.RaiseEvent(args);
            });
        }
        finally
        {
            WpfTestHost.ResetApplicationState();
        }
    }

    #endregion

    #region D-16 PasswordHelper

    [STATestMethod]
    public void PasswordHelper_SetPasswordOnANonPasswordBox_IsIgnoredInsteadOfThrowing()
    {
        var textBox = new TextBox();

        // 修复前：OnPasswordPropertyChanged 里 sender as PasswordBox 为 null，随后 NullReferenceException
        PasswordHelper.SetPassword(textBox, "x");

        Assert.AreEqual("x", PasswordHelper.GetPassword(textBox));
    }

    [STATestMethod]
    public void PasswordHelper_PasswordSetOnTheAttachedBox_ReachesTheBoxAndBack()
    {
        var box = new PasswordBox();
        PasswordHelper.SetAttach(box, true);

        PasswordHelper.SetPassword(box, "secret");
        Assert.AreEqual("secret", box.Password);

        box.Password = "typed";
        Assert.AreEqual("typed", PasswordHelper.GetPassword(box));
    }

    #endregion

    #region D-12 BrowseForFolderDialog 的 ANSI 路径

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int SHParseDisplayName(string pszName, IntPtr pbc, out IntPtr ppidl, uint sfgaoIn, out uint psfgaoOut);

    [DllImport("kernel32.dll")]
    private static extern uint GetACP();

    [TestMethod]
    public void BrowseForFolderDialog_PathFromPidl_KeepsCharactersOutsideTheAnsiCodePage()
    {
        if (GetACP() == 65001)
        {
            Assert.Inconclusive("系统 ANSI 代码页是 UTF-8，无法复现 ANSI 往返丢字符");
        }

        // U+1F600 不在任何传统 ANSI 代码页里（包括 936/GBK）
        string directory = Path.Combine(Path.GetTempPath(), "snet-pidl-" + Guid.NewGuid().ToString("N") + "-\U0001F600");
        Directory.CreateDirectory(directory);
        IntPtr pidl = IntPtr.Zero;
        try
        {
            Assert.AreEqual(0, SHParseDisplayName(directory, IntPtr.Zero, out pidl, 0, out _));

            MethodInfo getPath = typeof(Snet.Windows.Controls.property.wpf.Shell32.BrowseForFolderDialog)
                .GetMethod("SHGetPathFromIDList", BindingFlags.NonPublic | BindingFlags.Static)!;
            var buffer = new StringBuilder(260);
            Assert.IsTrue((bool)getPath.Invoke(null, [pidl, buffer])!);

            // 修复前：声明默认走 ANSI 入口，ANSI 代码页无法表示的字符被替换成 '?'
            Assert.AreEqual(Path.GetFileName(directory), Path.GetFileName(buffer.ToString()));
        }
        finally
        {
            if (pidl != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(pidl);
            }

            Directory.Delete(directory);
        }
    }

    [STATestMethod]
    public void BrowseForFolderDialog_OwnerHandle_IsResetOnlyWhenTheDialogAssignedIt()
    {
        var dialog = new Snet.Windows.Controls.property.wpf.Shell32.BrowseForFolderDialog();
        MethodInfo applyOwner = typeof(Snet.Windows.Controls.property.wpf.Shell32.BrowseForFolderDialog)
            .GetMethod("ApplyOwner", BindingFlags.Instance | BindingFlags.NonPublic)!;

        // 调用方通过公开的 BrowseInfo 自己设置的 hwndOwner（例如非 WPF 窗口的句柄）在没有 owner 的 ShowDialog() 里不能被清掉
        dialog.BrowseInfo.hwndOwner = new IntPtr(1234);
        applyOwner.Invoke(dialog, [null]);
        Assert.AreEqual(new IntPtr(1234), dialog.BrowseInfo.hwndOwner);

        // ShowDialog(owner) 之后再 ShowDialog()：上一次由本类写入的 owner 句柄（那个窗口可能已经销毁）要复位
        var window = new Window();
        IntPtr handle = new WindowInteropHelper(window).EnsureHandle();
        try
        {
            applyOwner.Invoke(dialog, [window]);
            Assert.AreEqual(handle, dialog.BrowseInfo.hwndOwner);

            applyOwner.Invoke(dialog, [null]);
            Assert.AreEqual(IntPtr.Zero, dialog.BrowseInfo.hwndOwner);
        }
        finally
        {
            window.Close();
        }
    }

    #endregion
}
