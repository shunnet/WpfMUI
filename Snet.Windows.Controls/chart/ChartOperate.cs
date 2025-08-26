using MaterialDesignThemes.Wpf;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using Snet.Core.extend;
using Snet.Core.handler;
using Snet.Model.data;
using Snet.Model.@enum;
using Snet.Windows.Core.@enum;
using Snet.Windows.Core.handler;
using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media.Imaging;
using static Snet.Windows.Controls.chart.ChartData;
using Image = ScottPlot.Image;
using MessageBox = Snet.Windows.Controls.message.MessageBox;
using MessageBoxButton = Snet.Windows.Controls.@enum.MessageBoxButton;
using MessageBoxImage = Snet.Windows.Controls.@enum.MessageBoxImage;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
namespace Snet.Windows.Controls.chart
{
    /// <summary>
    /// 图表操作类<br/>
    /// 一个操作类可实现一个图表对象的操作<br/>
    /// 支持在一个图表中添加多个动态的实时曲线<br/>
    /// 支持加载历史数据
    /// </summary>
    public class ChartOperate : CoreUnify<ChartOperate, ChartData.Basics>, IDisposable
    {
        /// <summary>
        /// 无参构造函数
        /// </summary>
        public ChartOperate() : base() { }

        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="basics">基础数据</param>
        public ChartOperate(ChartData.Basics basics) : base(basics) { }

        /// <summary>
        /// 皮肤切换事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SkinHandler_OnSkinEvent(object? sender, Core.data.EventSkinResult e)
        {
            Style(e.Skin ??= SkinType.Dark, wpfPlot);
        }

        #region 接口重写参数
        /// <inheritdoc/>
        protected override string CD => "支持图表的部分操作";

        /// <inheritdoc/>
        protected override string CN => "Chart";

        /// <inheritdoc/>
        public override LanguageModel LanguageOperate { get; set; } = new("Snet.Windows.Controls", "Language", "Snet.Windows.Controls.dll");

        /// <inheritdoc/>
        public override void Dispose()
        {
            Off();
            base.Dispose();
        }
        /// <inheritdoc/>
        public override async Task DisposeAsync()
        {
            Off();
            await base.DisposeAsync();
        }
        #endregion

        /// <summary>
        /// 图表控件
        /// </summary>
        private WpfPlot wpfPlot;

        /// <summary>
        /// 黑夜模式
        /// </summary>
        private ScottPlot.PlotStyles.Dark dark;

        /// <summary>
        /// 白天模式
        /// </summary>
        private ScottPlot.PlotStyles.Light light;

        /// <summary>
        /// 当前皮肤类型
        /// </summary>
        private SkinType CurrentSkinType;

        /// <summary>
        /// 如果使用了Edge 这个就不为空
        /// </summary>
        private ScottPlot.Panels.LegendPanel legendPanel;

        /// <summary>
        /// 默认模式则此不为空
        /// </summary>
        private Legend legend;

        /// <summary>
        /// 最大的实时线条数量
        /// </summary>
        private int MaxDataLoggerNum = 10;

        /// <summary>
        /// 数据流图表管理
        /// </summary>
        private ConcurrentDictionary<string, ChartData.DataLoggerSource> DataLoggerChartManage = new ConcurrentDictionary<string, ChartData.DataLoggerSource>();

        /// <summary>
        /// 自动刷新的token源
        /// </summary>
        private CancellationTokenSource AutoRefreshTokenSource;
        /// <summary>
        /// 自动刷新状态
        /// </summary>
        private bool AutoRefreshStatus = false;
        /// <summary>
        /// 自动刷新
        /// </summary>
        private async Task AutoRefreshAsync(CancellationToken token, int millisecond)
        {
            if (AutoRefreshStatus)
                return;
            try
            {
                await Task.Run(async () =>
                {
                    AutoRefreshStatus = true;
                    try
                    {
                        while (!token.IsCancellationRequested)
                        {
                            if (wpfPlot?.Plot.GetPlottables().Count() > 0)
                            {
                                wpfPlot?.Refresh();
                            }
                            await Task.Delay(millisecond, token);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        AutoRefreshStatus = false;
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        AutoRefreshStatus = false;
                        return;
                    }
                    catch (Exception)
                    {
                        AutoRefreshStatus = false;
                        return;
                    }
                }, token);
            }
            catch (TaskCanceledException)
            {
                AutoRefreshStatus = false;
                return;
            }
            catch (OperationCanceledException)
            {
                AutoRefreshStatus = false;
                return;
            }
            catch (Exception)
            {
                AutoRefreshStatus = false;
                return;
            }
        }
        /// <summary>
        /// 重置
        /// </summary>
        private void Reset()
        {
            //对象赋值
            wpfPlot ??= basics.ChartControl;
            wpfPlot.Reset();
            switch (GetLanguage())
            {
                case Snet.Model.@enum.LanguageType.zh:
                    //窗体XY轴标题
                    wpfPlot.Plot.XLabel(basics.XTitle ?? string.Empty, 13);
                    wpfPlot.Plot.YLabel(basics.YTitle ?? string.Empty, 13);
                    //设置字体
                    wpfPlot.Plot.Legend.FontName = ScottPlot.Fonts.Detect("宋体");
                    wpfPlot.Plot.Font.Set(ScottPlot.Fonts.Detect("宋体"));
                    break;
                case Snet.Model.@enum.LanguageType.en:
                    //窗体XY轴标题
                    wpfPlot.Plot.XLabel(basics.XTitleEN ?? string.Empty, 13);
                    wpfPlot.Plot.YLabel(basics.YTitleEN ?? string.Empty, 13);
                    break;
            }
            if (basics.LegendRight)
            {
                legendPanel = wpfPlot.Plot.ShowLegend(Edge.Right);
            }
            else
            {
                legend = wpfPlot.Plot.ShowLegend(Alignment.UpperRight);
            }
            if (basics.HideGrid)
            {
                wpfPlot.Plot.HideGrid();
            }

            OnLanguageEventAsync -= ChartOperate_OnLanguageEventAsync;
            OnLanguageEventAsync += ChartOperate_OnLanguageEventAsync;

            //这是默认值
            DefaultMenu(wpfPlot);
            SkinHandler.OnSkinEvent -= SkinHandler_OnSkinEvent;
            SkinHandler.OnSkinEvent += SkinHandler_OnSkinEvent;
            //刷新
            if (AutoRefreshTokenSource == null)
            {
                AutoRefreshTokenSource = new CancellationTokenSource();
                AutoRefreshAsync(AutoRefreshTokenSource.Token, basics.RefreshTime);
            }
        }

        /// <summary>
        /// 打开
        /// </summary>
        /// <returns>统一出参</returns>
        public OperateResult On()
        {
            BegOperate();
            try
            {
                if (GetStatus().GetDetails(out string? message))
                {
                    return EndOperate(false, message);
                }
                Reset();
                return EndOperate(true);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// 关闭
        /// </summary>
        /// <returns>统一出参</returns>
        public OperateResult Off()
        {
            BegOperate();
            try
            {
                if (!GetStatus().GetDetails(out string? message))
                {
                    return EndOperate(false, message);
                }

                if (AutoRefreshTokenSource != null)
                {
                    AutoRefreshStatus = false;
                    AutoRefreshTokenSource.Cancel();
                    AutoRefreshTokenSource = null;
                }

                //释放管理器
                DataLoggerChartManage.Where(c => { c.Value.Clear(); return true; });
                DataLoggerChartManage.Clear();

                //清空所有数据
                wpfPlot?.Plot?.Clear();
                Reset(wpfPlot?.Plot);
                wpfPlot?.Plot?.PlotControl?.Refresh();
                wpfPlot = null;

                return EndOperate(true);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// 获取状态
        /// </summary>
        /// <returns></returns>
        public OperateResult GetStatus()
        {
            BegOperate();
            try
            {
                if (wpfPlot != null)
                {
                    return EndOperate(true);
                }
                return EndOperate(false);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// 创建数据
        /// </summary>
        /// <param name="model">模型</param>
        /// <returns>统一出参</returns>
        public OperateResult Create(DataLoggerModel model)
        {
            BegOperate();
            try
            {
                if (!GetStatus().GetDetails(out string? message))
                {
                    return EndOperate(false, message);
                }

                if (!DataLoggerChartManage.ContainsKey(model.SN))
                {
                    if (DataLoggerChartManage.Count() < MaxDataLoggerNum)
                    {
                        //第一次创建
                        DataLogger obj = wpfPlot.Plot.Add.DataLogger();
                        switch (GetLanguage())
                        {
                            case Snet.Model.@enum.LanguageType.zh:
                                //设置线条标题
                                obj.LegendText = model.Title ?? string.Empty;
                                break;
                            case Snet.Model.@enum.LanguageType.en:
                                //设置线条标题
                                obj.LegendText = model.TitleEN ?? string.Empty;
                                break;
                        }
                        obj.Color = model?.Color == null ? wpfPlot.Plot.Add.GetNextColor() : new ScottPlot.Color(model.Color);
                        obj.LineWidth = model.Width;
                        obj.ViewSlide();
                        ChartData.DataLoggerSource source = new()
                        {
                            model = model,
                            logger = obj,
                            plot = wpfPlot
                        };
                        //添加到对象
                        DataLoggerChartManage.TryAdd(model.SN, source);
                        return EndOperate(true);
                    }
                    return EndOperate(false, LanguageOperate.GetLanguageValue("超过最大实时线条限制，实时线条过多会导致性能大幅度下降"));
                }
                return EndOperate(false, $"{model.SN}{LanguageOperate.GetLanguageValue("已存在")}");
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// 移除指定Sn的线条，线条在空间中不存在
        /// </summary>
        /// <param name="sn">Create model 中的SN</param>
        /// <returns>统一出参</returns>
        public OperateResult Remove(string sn)
        {
            BegOperate();
            try
            {
                if (!GetStatus().GetDetails(out string? message))
                {
                    return EndOperate(false, message);
                }
                if (DataLoggerChartManage.Remove(sn, out var data))
                {
                    if (Clear(sn).GetDetails(out message))
                    {
                        //把此线条从控件中移除
                        data.plot.Plot.Remove(data.logger);
                        return EndOperate(true);
                    }
                    return EndOperate(false, message);
                }
                return EndOperate(false, $"{sn}{LanguageOperate.GetLanguageValue("不存在")}");
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// 只获取实时采集的数据
        /// </summary>
        /// <param name="sn">Create model 中的SN</param>
        /// <returns></returns>
        public OperateResult GetValue(string sn)
        {
            BegOperate();
            try
            {
                if (!GetStatus().GetDetails(out string? message))
                {
                    return EndOperate(false, message);
                }
                if (DataLoggerChartManage.TryGetValue(sn, out var data))
                {
                    return EndOperate(true, resultData: data.Get());
                }
                return EndOperate(false, $"{sn}{LanguageOperate.GetLanguageValue("不存在")}");
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="sn">Create model 中的SN</param>
        /// <param name="value">值</param>
        /// <returns>统一出参</returns>
        public OperateResult Update(string sn, double value)
        {
            BegOperate();
            try
            {
                if (DataLoggerChartManage.TryGetValue(sn, out var data))
                {
                    data.Update(value);
                    return EndOperate(true);
                }
                return EndOperate(false, $"{sn}{LanguageOperate.GetLanguageValue("不存在")}");
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// 情况指定SN的线条数据
        /// </summary>
        /// <param name="sn">Create model 中的SN</param>
        /// <returns></returns>
        public OperateResult Clear(string sn)
        {
            BegOperate();
            try
            {
                if (DataLoggerChartManage.TryGetValue(sn, out var data))
                {
                    data.Clear();
                    return EndOperate(true);
                }
                return EndOperate(false, $"{sn}{LanguageOperate.GetLanguageValue("不存在")}");
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// 获取指定SN线条的数据
        /// </summary>
        /// <param name="sn">Create model 中的SN</param>
        /// <returns></returns>
        public OperateResult Get(string sn)
        {
            BegOperate();
            try
            {
                if (DataLoggerChartManage.TryGetValue(sn, out var data))
                {
                    return EndOperate(true, resultData: data.Get());
                }
                return EndOperate(false, $"{sn}{LanguageOperate.GetLanguageValue("不存在")}");
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// 将 WPF 的 Color 转换为 WinForms 的 Color
        /// </summary>
        /// <param name="color">WPF 的 System.Windows.Media.Color</param>
        /// <returns>WinForms 的 System.Drawing.Color</returns>
        private System.Drawing.Color ToDrawingColor(System.Windows.Media.Color color)
        {
            // 使用 FromArgb，按照 A (透明度), R (红), G (绿), B (蓝) 顺序生成
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        /// <summary>
        /// 样式
        /// </summary>
        /// <param name="skin">皮肤</param>
        /// <param name="plot">控件对象</param>
        /// <returns>成功失败</returns>
        public bool Style(SkinType skin, WpfPlot? plot = null)
        {
            try
            {
                if (plot == null)
                {
                    plot = wpfPlot;
                }
                CurrentSkinType = skin;
                switch (skin)
                {
                    case SkinType.Dark:
                        if (dark == null)
                        {
                            dark = new ScottPlot.PlotStyles.Dark()
                            {
                                ////轴的颜色
                                //AxisColor = new(((SolidColorBrush)System.Windows.Application.Current.TryFindResource("MaterialDesignBodyLight")).Color.ToDrawingColor()),
                                ////网格颜色
                                //GridMajorLineColor = new(((SolidColorBrush)System.Windows.Application.Current.TryFindResource("MaterialDesignDivider")).Color.ToDrawingColor()),
                                //背景色
                                FigureBackgroundColor = new(ToDrawingColor(System.Windows.Media.Color.FromArgb(0, 255, 0, 0))),
                                DataBackgroundColor = new(ToDrawingColor(System.Windows.Media.Color.FromArgb(0, 255, 0, 0))),
                                //LegendBackgroundColor = new((System.Windows.Media.Color.FromArgb(0, 255, 0, 0)).ToDrawingColor()),
                                //图例字体颜色
                                //LegendFontColor = new(((SolidColorBrush)System.Windows.Application.Current.TryFindResource("MaterialDesignBody")).Color.ToDrawingColor()),
                                //图例轮廓颜色
                                //LegendOutlineColor = new(((SolidColorBrush)System.Windows.Application.Current.TryFindResource("MaterialDesignDivider")).Color.ToDrawingColor())
                            };
                        }
                        plot.Plot.SetStyle(dark);
                        break;
                    case SkinType.Light:
                        if (light == null)
                        {
                            light = new ScottPlot.PlotStyles.Light()
                            {
                                ////轴的颜色
                                //AxisColor = new(((SolidColorBrush)System.Windows.Application.Current.TryFindResource("MaterialDesignBodyLight")).Color.ToDrawingColor()),
                                ////网格颜色
                                //GridMajorLineColor = new(((SolidColorBrush)System.Windows.Application.Current.TryFindResource("MaterialDesignDivider")).Color.ToDrawingColor()),
                                //背景色
                                FigureBackgroundColor = new(ToDrawingColor(System.Windows.Media.Color.FromArgb(0, 255, 0, 0))),
                                DataBackgroundColor = new(ToDrawingColor(System.Windows.Media.Color.FromArgb(0, 255, 0, 0))),
                                //LegendBackgroundColor = new((System.Windows.Media.Color.FromArgb(0, 255, 0, 0)).ToDrawingColor()),
                                ////图例字体颜色
                                ////LegendFontColor = new(((SolidColorBrush)System.Windows.Application.Current.TryFindResource("MaterialDesignBody")).Color.ToDrawingColor()),
                                ////图例轮廓颜色
                                ////LegendOutlineColor = new(((SolidColorBrush)System.Windows.Application.Current.TryFindResource("MaterialDesignDivider")).Color.ToDrawingColor())
                            };
                        }
                        plot.Plot.SetStyle(light);
                        break;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 如果语言切换则会进来
        /// </summary>
        private Task ChartOperate_OnLanguageEventAsync(object? sender, EventLanguageResult e)
        {
            if (wpfPlot == null)
                return Task.CompletedTask;
            if (e.GetDetails(out string msg, out LanguageType? language))
            {
                foreach (var item in DataLoggerChartManage)
                {
                    switch (language ??= GetLanguage())
                    {
                        case Snet.Model.@enum.LanguageType.zh:
                            //设置线条标题
                            item.Value.logger.LegendText = item.Value.model.Title ?? string.Empty;
                            break;
                        case Snet.Model.@enum.LanguageType.en:
                            //设置线条标题
                            item.Value.logger.LegendText = item.Value.model.TitleEN ?? string.Empty;
                            break;
                    }
                }
                switch (language ??= GetLanguage())
                {
                    case Snet.Model.@enum.LanguageType.zh:
                        //窗体XY轴标题
                        wpfPlot.Plot.XLabel(basics.XTitle ?? string.Empty, 13);
                        wpfPlot.Plot.YLabel(basics.YTitle ?? string.Empty, 13);
                        //设置字体
                        wpfPlot.Plot.Legend.FontName = ScottPlot.Fonts.Detect("宋体");
                        wpfPlot.Plot.Font.Set(ScottPlot.Fonts.Detect("宋体"));
                        break;
                    case Snet.Model.@enum.LanguageType.en:
                        //窗体XY轴标题
                        wpfPlot.Plot.XLabel(basics.XTitleEN ?? string.Empty, 13);
                        wpfPlot.Plot.YLabel(basics.YTitleEN ?? string.Empty, 13);
                        break;
                }
                DefaultMenu(wpfPlot);
                wpfPlot.Refresh();
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 默认右键菜单项
        /// </summary>
        /// <param name="plot">控件对象</param>
        /// <returns>成功失败</returns>
        public bool DefaultMenu(WpfPlot plot)
        {
            try
            {
                if (plot != null)
                {
                    // 清空默认右键菜单
                    plot.Menu?.Clear();


                    plot.Menu?.Add(LanguageOperate.GetLanguageValue("调整"), Adjust);
                    plot.Menu?.AddSeparator();
                    plot.Menu?.Add(LanguageOperate.GetLanguageValue("重置"), Reset);
                    plot.Menu?.AddSeparator();
                    plot.Menu?.Add(LanguageOperate.GetLanguageValue("保存图片"), SaveImage);
                    plot.Menu?.AddSeparator();
                    plot.Menu?.Add(LanguageOperate.GetLanguageValue("复制图片"), CopyImage);
                    plot.Menu?.AddSeparator();
                    plot.Menu?.Add(LanguageOperate.GetLanguageValue("移除所有线条"), RemoveAll);
                    if (basics.LineAdjust)
                    {
                        plot.Menu?.AddSeparator();
                        plot.Menu?.Add(LanguageOperate.GetLanguageValue("线条操作"), LineOperate);
                    }
                    return true;
                }
            }
            catch { }
            return false;
        }

        #region 右键菜单项

        /// <summary>
        /// 保存图片
        /// </summary>
        /// <param name="plot">绘图对象</param>
        private void SaveImage(Plot plot)
        {
            SaveFileDialog dialog = new()
            {
                FileName = "chart.png",
                Filter = "PNG Files (*.png)|*.png" +
                         "|JPEG Files (*.jpg, *.jpeg)|*.jpg;*.jpeg" +
                         "|BMP Files (*.bmp)|*.bmp" +
                         "|WebP Files (*.webp)|*.webp" +
                         "|SVG Files (*.svg)|*.svg" +
                         "|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() is true)
            {
                if (string.IsNullOrEmpty(dialog.FileName))
                    return;

                ImageFormat format;

                try
                {
                    format = ImageFormats.FromFilename(dialog.FileName);
                }
                catch (ArgumentException)
                {
                    MessageBox.Show(LanguageOperate.GetLanguageValue("不支持的图像文件格式"), LanguageOperate.GetLanguageValue("异常"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                try
                {
                    PixelSize lastRenderSize = plot.RenderManager.LastRender.FigureRect.Size;
                    plot.Save(dialog.FileName, (int)lastRenderSize.Width, (int)lastRenderSize.Height, format);
                }
                catch (Exception)
                {
                    MessageBox.Show(LanguageOperate.GetLanguageValue("图像保存失败"), LanguageOperate.GetLanguageValue("异常"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
        }
        /// <summary>
        /// 复制图片
        /// </summary>
        /// <param name="plot">绘图对象</param>
        private void CopyImage(Plot plot)
        {
            PixelSize lastRenderSize = plot.RenderManager.LastRender.FigureRect.Size;
            Image bmp = plot.GetImage((int)lastRenderSize.Width, (int)lastRenderSize.Height);
            byte[] bmpBytes = bmp.GetImageBytes();

            using MemoryStream ms = new();
            ms.Write(bmpBytes, 0, bmpBytes.Length);
            BitmapImage bmpImage = new();
            bmpImage.BeginInit();
            bmpImage.StreamSource = ms;
            bmpImage.EndInit();
            System.Windows.Clipboard.SetImage(bmpImage);
        }

        /// <summary>
        /// 调整
        /// </summary>
        /// <param name="plot">绘图对象</param>
        private void Adjust(Plot plot)
        {
            plot.Axes.AutoScale();
            plot.PlotControl?.Refresh();
        }

        /// <summary>
        /// 重置
        /// </summary>
        /// <param name="plot">绘图对象</param>
        private void Reset(Plot plot)
        {
            plot.Axes.SetLimitsX(-10, 10);
            plot.Axes.SetLimitsY(-10, 10);
            plot.PlotControl?.Refresh();
        }

        /// <summary>
        /// 移除所有线条
        /// </summary>
        /// <param name="plot"></param>
        private void RemoveAll(Plot plot)
        {
            if (plot == null)
                return;
            // 清空所有数据
            plot.Clear();
            Reset(plot);
            Reset();
            switch (CurrentSkinType)
            {
                case SkinType.Dark: wpfPlot.Plot.SetStyle(dark); break;
                case SkinType.Light: wpfPlot.Plot.SetStyle(light); break;
            }
            plot.PlotControl?.Refresh();
        }

        /// <summary>
        /// 线条操作
        /// </summary>
        /// <param name="plot">绘图对象</param>
        private void LineOperate(Plot plot)
        {
            plot.Legend.ShowItemsFromHiddenPlottables = true;
            plot.Legend.OutlineWidth = 0;
            plot.Legend.BackgroundColor = ScottPlot.Color.FromSDColor(System.Drawing.SystemColors.Control);
            plot.Legend.ShadowColor = ScottPlot.Colors.Transparent;
            if (legend != null)
            {
                legend.IsVisible = false;
            }
            wpfPlot.Refresh();

            ChartLine form = new ChartLine()
            {
                Width = wpfPlot.Plot.Legend.LastRenderSize.Width,
                Height = wpfPlot.Plot.Legend.LastRenderSize.Height
            };

            SKElement sKElement = new()
            {
                Width = wpfPlot.Plot.Legend.LastRenderSize.Width,
                Height = wpfPlot.Plot.Legend.LastRenderSize.Height
            };
            sKElement.PaintSurface += (s, e) => { PaintDetachedLegend((SKElement)s!, e); };
            sKElement.MouseLeftButtonDown += (s, e) => { MouseLeftButtonDown((SKElement)s!, e); };
            form.Content = sKElement;
            form.Opacity = 0;
            void Loaded(object? sender, DialogOpenedEventArgs eventArgs)
            {
                switch (CurrentSkinType)
                {
                    case SkinType.Dark: wpfPlot.Plot.SetStyle(dark); break;
                    case SkinType.Light: wpfPlot.Plot.SetStyle(light); break;
                }
                wpfPlot.Refresh();
                form.Opacity = 1;
            }
            void Closing(object? sender, DialogClosingEventArgs eventArgs)
            {
                if (legend != null)
                {
                    legend.IsVisible = true;
                    legend.OutlineWidth = 1;
                }

                switch (CurrentSkinType)
                {
                    case SkinType.Dark: wpfPlot.Plot.SetStyle(dark); break;
                    case SkinType.Light: wpfPlot.Plot.SetStyle(light); break;
                }
                wpfPlot.Refresh();
            }

            DialogHost.Show(form, "ChartLineOperate", Loaded, Closing);


        }
        private void PaintDetachedLegend(SKElement sender, SKPaintSurfaceEventArgs e)
        {
            PixelSize size = new(sender.Width, sender.Height);
            PixelRect rect = new(Pixel.Zero, size);
            SKCanvas canvas = e.Surface.Canvas;
            wpfPlot.Plot.Legend.Render(canvas, rect, Alignment.UpperLeft);
        }
        private void MouseLeftButtonDown(SKElement sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var item = GetLegendItemUnderMouse(sender, e.GetPosition(sender));
            var ClickedPlottable = item != null ? item.Plottable : null;
            if (ClickedPlottable != null)
            {
                ClickedPlottable.IsVisible = !ClickedPlottable.IsVisible;
            }
            wpfPlot.Refresh();
            sender.InvalidateVisual();
        }
        private LegendItem? GetLegendItemUnderMouse(SKElement sender, System.Windows.Point e)
        {
            PixelSize size = new(sender.Width, sender.Height);
            LegendItem[] items = wpfPlot.Plot.Legend.GetItems();
            LegendLayout layout = wpfPlot.Plot.Legend.GetLayout(size);
            if (items.Count() == 0)
                return null;
            var itemslayout = Enumerable.Zip(items, layout.LabelRects, layout.SymbolRects);
            foreach (var il in itemslayout)
            {
                var item = il.First;
                var lrect = il.Second;
                var srect = il.Third;
                if (lrect.Contains((float)e.X, (float)e.Y) || srect.Contains((float)e.X, (float)e.Y))
                {
                    return item;
                }
            }
            return null;
        }

        #endregion

    }
}
