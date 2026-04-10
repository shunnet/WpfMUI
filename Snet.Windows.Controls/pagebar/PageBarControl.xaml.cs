using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Snet.Windows.Controls.pagebar
{
    /// <summary>
    /// 页码条控件<br/>
    /// 支持数据分页导航，包括上一页/下一页、页码跳转、省略号和每页大小设置<br/>
    /// 通过缓存 PageBarItem 实例减少重复创建，提高性能
    /// </summary>
    [TemplatePart(Name = PageSizePartName, Type = typeof(TextBox))]
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(PageBarItem))]
    public partial class PageBarControl : ItemsControl
    {
        /// <summary>
        /// 构造函数：初始化页码条控件组件和异步翻页命令
        /// </summary>
        public PageBarControl()
        {
            InitializeComponent();
            _pageNumberCommand = new AsyncRelayCommand<object>(PageNumberChangedAsync);
        }

        #region 常量

        /// <summary>页大小输入框模板部件名称</summary>
        public const string PageSizePartName = "PART_PageSizeTextBox";

        #endregion

        #region 缓存 & 命令

        /// <summary>页码项缓存列表，避免重复创建 PageBarItem 实例</summary>
        private readonly List<PageBarItem> _pageItemCache = new();
        /// <summary>异步翻页命令</summary>
        private readonly IAsyncRelayCommand<object> _pageNumberCommand;

        #endregion

        #region 静态构造

        /// <summary>
        /// 静态构造函数：重写默认样式键以支持自定义模板
        /// </summary>
        static PageBarControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PageBarControl), new FrameworkPropertyMetadata(typeof(PageBarControl)));
        }

        #endregion

        #region 模板

        /// <summary>
        /// 应用模板时初始化页大小输入框的回车键事件
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild(PageSizePartName) is TextBox pageSizeTextBox)
            {
                pageSizeTextBox.KeyDown += (sender, e) =>
                {
                    if (e.Key == Key.Enter)
                        this.Focus();
                };
            }
        }

        protected override bool IsItemItsOwnContainerOverride(object item) => item is PageBarItem;

        protected override DependencyObject GetContainerForItemOverride() => new PageBarItem();

        #endregion

        #region 依赖属性

        /// <summary>最大显示页码数量依赖属性，默认为 7</summary>
        public static readonly DependencyProperty MaxDisplayedPageCountProperty =
            DependencyProperty.Register("MaxDisplayedPageCount", typeof(int), typeof(PageBarControl),
                new FrameworkPropertyMetadata(7, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnMaxDisplayedPageCountChanged));

        /// <summary>
        /// 获取或设置页码条中最多显示的页码按钮数量
        /// </summary>
        public int MaxDisplayedPageCount
        {
            get => (int)GetValue(MaxDisplayedPageCountProperty);
            set => SetValue(MaxDisplayedPageCountProperty, value);
        }

        /// <summary>最大显示页码数量变更回调，刷新页码条</summary>
        private static void OnMaxDisplayedPageCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pageBar = (PageBarControl)d;
            _ = pageBar.RefreshPageBarAsync();
        }

        /// <summary>每页数据量依赖属性</summary>
        public static readonly DependencyProperty PageSizeProperty =
            DependencyProperty.Register("PageSize", typeof(int), typeof(PageBarControl),
                new FrameworkPropertyMetadata(default(int), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPageSizeChanged));

        /// <summary>
        /// 获取或设置每页显示的数据条数
        /// </summary>
        public int PageSize
        {
            get => (int)GetValue(PageSizeProperty);
            set => SetValue(PageSizeProperty, value);
        }

        /// <summary>每页大小变更回调，重置页码并刷新页码条</summary>
        private static void OnPageSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pageBar = (PageBarControl)d;
            pageBar.PageIndex = 1;
            _ = pageBar.RefreshPageBarAsync();
            pageBar.RaisePageSizeChanged((int)e.OldValue, (int)e.NewValue);
        }

        /// <summary>当前页码依赖属性，默认为 1</summary>
        public static readonly DependencyProperty PageIndexProperty =
            DependencyProperty.Register("PageIndex", typeof(int), typeof(PageBarControl),
                new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPageIndexChanged));

        /// <summary>
        /// 获取或设置当前页码（从 1 开始）
        /// </summary>
        public int PageIndex
        {
            get => (int)GetValue(PageIndexProperty);
            set => SetValue(PageIndexProperty, value);
        }

        /// <summary>页码变更回调，刷新页码条并触发事件</summary>
        private static void OnPageIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pageBar = (PageBarControl)d;
            _ = pageBar.RefreshPageBarAsync();
            pageBar.RaisePageIndexChanged((int)e.OldValue, (int)e.NewValue);
        }

        /// <summary>数据总条数依赖属性</summary>
        public static readonly DependencyProperty TotalProperty =
            DependencyProperty.Register("Total", typeof(int), typeof(PageBarControl),
                new FrameworkPropertyMetadata(default(int), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTotalChanged));

        /// <summary>
        /// 获取或设置数据总条数，用于计算总页数
        /// </summary>
        public int Total
        {
            get => (int)GetValue(TotalProperty);
            set => SetValue(TotalProperty, value);
        }

        /// <summary>总数变更回调，刷新页码条</summary>
        private static void OnTotalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pageBar = (PageBarControl)d;
            _ = pageBar.RefreshPageBarAsync();
        }

        #endregion

        #region 事件 & 命令

        /// <summary>页大小变更路由事件</summary>
        public static readonly RoutedEvent PageSizeChangedEvent =
            EventManager.RegisterRoutedEvent("PageSizeChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<int>), typeof(PageBarControl));

        /// <summary>页大小变更事件</summary>
        public event RoutedPropertyChangedEventHandler<int> PageSizeChanged
        {
            add => AddHandler(PageSizeChangedEvent, value);
            remove => RemoveHandler(PageSizeChangedEvent, value);
        }

        /// <summary>页码变更路由事件</summary>
        public static readonly RoutedEvent PageIndexChangedEvent =
            EventManager.RegisterRoutedEvent("PageIndexChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<int>), typeof(PageBarControl));

        /// <summary>页码变更事件</summary>
        public event RoutedPropertyChangedEventHandler<int> PageIndexChanged
        {
            add => AddHandler(PageIndexChangedEvent, value);
            remove => RemoveHandler(PageIndexChangedEvent, value);
        }

        /// <summary>页大小变更命令依赖属性</summary>
        public static readonly DependencyProperty PageSizeChangedCommandProperty =
            DependencyProperty.Register("PageSizeChangedCommand", typeof(ICommand), typeof(PageBarControl), new PropertyMetadata(default(ICommand)));

        /// <summary>
        /// 获取或设置页大小变更时执行的命令
        /// </summary>
        public ICommand PageSizeChangedCommand
        {
            get => (ICommand)GetValue(PageSizeChangedCommandProperty);
            set => SetValue(PageSizeChangedCommandProperty, value);
        }

        /// <summary>页码变更命令依赖属性</summary>
        public static readonly DependencyProperty PageIndexChangedCommandProperty =
            DependencyProperty.Register("PageIndexChangedCommand", typeof(ICommand), typeof(PageBarControl), new PropertyMetadata(default(ICommand)));

        /// <summary>
        /// 获取或设置页码变更时执行的命令
        /// </summary>
        public ICommand PageIndexChangedCommand
        {
            get => (ICommand)GetValue(PageIndexChangedCommandProperty);
            set => SetValue(PageIndexChangedCommandProperty, value);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 触发页大小变更事件和命令
        /// </summary>
        /// <param name="oldValue">旧的页大小</param>
        /// <param name="newValue">新的页大小</param>
        private void RaisePageSizeChanged(int oldValue, int newValue)
        {
            var args = new RoutedPropertyChangedEventArgs<int>(oldValue, newValue)
            {
                RoutedEvent = PageSizeChangedEvent
            };
            RaiseEvent(args);
            PageSizeChangedCommand?.Execute(newValue);
        }

        /// <summary>
        /// 触发页码变更事件和命令
        /// </summary>
        /// <param name="oldValue">旧的页码</param>
        /// <param name="newValue">新的页码</param>
        private void RaisePageIndexChanged(int oldValue, int newValue)
        {
            var args = new RoutedPropertyChangedEventArgs<int>(oldValue, newValue)
            {
                RoutedEvent = PageIndexChangedEvent
            };
            RaiseEvent(args);
            PageIndexChangedCommand?.Execute(newValue);
        }

        /// <summary>
        /// 页码点击回调，更新当前页码
        /// </summary>
        /// <param name="obj">点击的页码值</param>
        private Task PageNumberChangedAsync(object obj)
        {
            if (obj is int num)
                PageIndex = num;
            return Task.CompletedTask;
        }

        /// <summary>
        /// 异步刷新页码条
        /// </summary>
        private async Task RefreshPageBarAsync()
        {
            await Task.Yield(); // 放到 UI 队列尾，避免阻塞
            ReFreshPageBar();
        }

        /// <summary>
        /// 核心刷新逻辑，使用缓存提高性能
        /// </summary>
        private void ReFreshPageBar()
        {
            if (PageSize <= 0 || Total <= 0)
                return;

            int pageCount = (int)Math.Ceiling(Total / (double)PageSize);
            if (pageCount <= 0) return;

            // 清空并隐藏缓存
            foreach (var item in _pageItemCache)
            {
                item.Visibility = Visibility.Collapsed;
                item.IsEnabled = true;
            }
            Items.Clear();

            // 上一页
            var previousPage = GetPageItem("＜", PageIndex - 1);
            previousPage.IsEnabled = PageIndex > 1;
            Items.Add(previousPage);

            // 首页
            Items.Add(GetPageItem("1", 1));

            // 如果总页数少，不需要复杂逻辑
            if (pageCount <= MaxDisplayedPageCount)
            {
                // 直接显示所有页码（从2到pageCount-1）
                for (int i = 2; i < pageCount; i++)
                {
                    Items.Add(GetPageItem(i.ToString(), i));
                }
            }
            else
            {
                // 需要省略号的复杂情况
                int maxMiddlePages = MaxDisplayedPageCount - 4; // 减去：上页、首页、尾页、下页
                int half = maxMiddlePages / 2;

                int leftEllipsis = -1, rightEllipsis = -1;

                // 计算中间显示的起始和结束页
                int start = Math.Max(2, PageIndex - half);
                int end = Math.Min(pageCount - 1, PageIndex + half);

                // 调整以保证中间页数尽量接近 maxMiddlePages
                if (end - start + 1 < maxMiddlePages)
                {
                    if (PageIndex <= half + 2)
                        end = Math.Min(pageCount - 1, start + maxMiddlePages - 1);
                    else if (PageIndex >= pageCount - half - 1)
                        start = Math.Max(2, end - maxMiddlePages + 1);
                }

                // 左省略号：如果 start > 2
                if (start > 2)
                    leftEllipsis = start - 1; // 代表插入位置前的页码

                // 右省略号：如果 end < pageCount - 1
                if (end < pageCount - 1)
                    rightEllipsis = end + 1;

                // 添加左省略号
                if (leftEllipsis != -1)
                    Items.Add(GetPageItem("...", leftEllipsis)); // value 无意义，可设为跳转用或固定

                // 添加中间页码
                for (int i = start; i <= end; i++)
                {
                    Items.Add(GetPageItem(i.ToString(), i));
                }

                // 添加右省略号
                if (rightEllipsis != -1)
                    Items.Add(GetPageItem("...", rightEllipsis));
            }

            // 尾页（只有当 pageCount > 1 时显示）
            if (pageCount > 1)
            {
                var lastPageItem = GetPageItem(pageCount.ToString(), pageCount);
                // 如果末尾已经有 end == pageCount-1，则不重复添加（但由于上面控制了不会）
                Items.Add(lastPageItem);
            }

            // 下一页
            var nextPage = GetPageItem("＞", PageIndex + 1);
            nextPage.IsEnabled = PageIndex < pageCount;
            Items.Add(nextPage);
        }



        /// <summary>
        /// 获取缓存按钮或创建新按钮
        /// </summary>
        private PageBarItem GetPageItem(string content, int value)
        {
            var item = _pageItemCache.FirstOrDefault(x => x.Value == value && x.Content?.ToString() == content);
            if (item == null)
            {
                item = new PageBarItem()
                {
                    Content = content,
                    Value = value,
                    PageNumberCommand = _pageNumberCommand
                };
                _pageItemCache.Add(item);
            }

            item.Visibility = Visibility.Visible;
            return item;
        }

        #endregion
    }
}
