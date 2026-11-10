// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PropertyGridControlFactory.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Provides a control factory for the PropertyGrid control.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Security;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Threading;

    /// <summary>
    /// Provides a control factory for the <see cref="PropertyGrid" /> control.
    /// </summary>
    public class PropertyGridControlFactory : IPropertyGridControlFactory
    {
        /// <summary>
        /// The font family converter
        /// </summary>
        private static readonly FontFamilyConverter FontFamilyConverter = new FontFamilyConverter();

        /// <summary>
        /// The cached font families.
        /// </summary>
        private static FontFamily[] cachedFontFamilies;

        /// <summary>
        /// 错误通知订阅表：每个 <see cref="INotifyDataErrorInfo"/> 实例只订阅一次 ErrorsChanged（键为弱引用，
        /// 实例销毁后条目自动回收），避免每行属性都订阅且永不退订导致的泄漏。
        /// </summary>
        private static readonly ConditionalWeakTable<INotifyDataErrorInfo, ErrorSubscriptionEntry> ErrorSubscriptions =
            new ConditionalWeakTable<INotifyDataErrorInfo, ErrorSubscriptionEntry>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyGridControlFactory" /> class.
        /// </summary>
        public PropertyGridControlFactory()
        {
            this.Converters = new List<PropertyConverter>();
            this.Editors = new List<TypeEditor>();
        }

        /// <summary>
        /// 登记表超过该数量后，下一次登记时顺带清理失效条目（避免对长寿命模型反复重建网格造成缓慢增长）。
        /// </summary>
        private const int InitialRegistrationPruneThreshold = 256;

        /// <summary>
        /// 订阅实例的 ErrorsChanged 事件（每个实例只订阅一次），并登记 (Tab, 属性名, 错误控件) 弱引用。<br/>
        /// 登记表是不可变数组快照，可以在任意线程上登记、读取（无锁）；每条登记记录自己的错误控件所在的 UI 线程，
        /// 所以同一个模型实例可以同时被多个 UI 线程上的属性网格显示。
        /// </summary>
        /// <param name="instance">实现 <see cref="INotifyDataErrorInfo"/> 的实例。</param>
        /// <param name="tab">所属 Tab。</param>
        /// <param name="propertyName">属性名。</param>
        /// <param name="errorControl">该属性行的错误控件，错误变化时需要强制刷新它的绑定。</param>
        private static void SubscribeErrorsChanged(INotifyDataErrorInfo instance, Tab tab, string propertyName, ContentControl errorControl)
        {
            var entry = ErrorSubscriptions.GetOrCreateValue(instance);
            entry.Add(new ErrorRegistration(tab, propertyName, errorControl));
            if (entry.TryMarkSubscribed())
            {
                instance.ErrorsChanged += OnErrorsChanged;
            }
        }

        /// <summary>
        /// 统一的 ErrorsChanged 处理器：更新与发生错误属性相关的活动 Tab（增量更新），并强制刷新这些属性行的错误控件。<br/>
        /// <see cref="INotifyDataErrorInfo.ErrorsChanged"/> 可能在任意线程触发（例如异步校验之后），
        /// 而 Tab 与错误控件只能在创建它们的 UI 线程上访问：命中的登记按各自控件所在的调度器分组，每一组在自己的 UI 线程上处理
        /// （已经结束的调度器上的那一组被丢弃，不影响其它线程上的网格）。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">事件参数。</param>
        private static void OnErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
        {
            var instance = sender as INotifyDataErrorInfo;
            if (instance == null || !ErrorSubscriptions.TryGetValue(instance, out var entry))
            {
                return;
            }

            string? propertyName = e.PropertyName;
            Dictionary<Dispatcher, List<ErrorRegistration>>? groups = null;
            foreach (var registration in entry.Registrations)
            {
                // PropertyName 为空表示对象整体错误变化，需要全量重算
                if (!string.IsNullOrEmpty(propertyName) && registration.PropertyName != propertyName)
                {
                    continue;
                }

                groups ??= new Dictionary<Dispatcher, List<ErrorRegistration>>();
                if (!groups.TryGetValue(registration.Dispatcher, out var group))
                {
                    group = new List<ErrorRegistration>();
                    groups.Add(registration.Dispatcher, group);
                }

                group.Add(registration);
            }

            if (groups == null)
            {
                return;
            }

            foreach (var pair in groups)
            {
                var registrations = pair.Value;
                if (pair.Key.CheckAccess())
                {
                    ApplyErrorsChanged(instance, registrations, propertyName);
                }
                else
                {
                    pair.Key.BeginInvoke(new Action(() => ApplyErrorsChanged(instance, registrations, propertyName)));
                }
            }
        }

        /// <summary>
        /// 在登记所属的 UI 线程上处理一次 ErrorsChanged：更新 Tab 的错误状态，并强制刷新各行的错误控件。
        /// </summary>
        /// <param name="instance">事件源。</param>
        /// <param name="registrations">同一个 UI 线程上的、与这次错误变化相关的登记。</param>
        /// <param name="propertyName">发生错误变化的属性名，空表示对象整体。</param>
        private static void ApplyErrorsChanged(INotifyDataErrorInfo instance, List<ErrorRegistration> registrations, string? propertyName)
        {
            var tabs = new HashSet<Tab>();
            var errorControls = new List<ContentControl>();
            foreach (var registration in registrations)
            {
                // Tab 已被回收：这条登记失效（登记表在下次登记超过阈值时清理）
                if (registration.TabReference.Target is not Tab tab)
                {
                    continue;
                }

                tabs.Add(tab);
                if (registration.ErrorControlReference.Target is ContentControl errorControl)
                {
                    errorControls.Add(errorControl);
                }
            }

            foreach (var tab in tabs)
            {
                tab.UpdateHasErrors(instance, propertyName);
            }

            // 错误控件的绑定源是 Tab.HasErrors，它只在 false↔true 翻转时才通知；
            // 同一个 Tab 里第二个属性出错、或其中一个属性改好而另一个仍然出错时 HasErrors 不变，必须手工刷新，
            // 否则提示会缺失（后出错的属性）或陈旧（已经改好的属性）
            foreach (var errorControl in errorControls)
            {
                errorControl.GetBindingExpression(UIElement.VisibilityProperty)?.UpdateTarget();
                errorControl.GetBindingExpression(ContentControl.ContentProperty)?.UpdateTarget();
            }
        }

        /// <summary>
        /// 记录一个实例的 ErrorsChanged 订阅状态及关联的 (Tab, 属性名, 错误控件)（弱引用）登记表。
        /// </summary>
        private sealed class ErrorSubscriptionEntry
        {
            /// <summary>
            /// 登记表：不可变数组，整体替换（<see cref="Interlocked.CompareExchange{T}(ref T, T, T)"/>），所以任何线程都可以无锁读取。
            /// </summary>
            private ErrorRegistration[] registrations = Array.Empty<ErrorRegistration>();

            /// <summary>
            /// 登记表条目数达到该值时，下一次登记先清理失效条目（避免对长寿命模型反复重建网格造成缓慢增长）。
            /// </summary>
            private int pruneThreshold = InitialRegistrationPruneThreshold;

            /// <summary>
            /// 是否已订阅 ErrorsChanged（0/1，用 <see cref="Interlocked"/> 置位）。
            /// </summary>
            private int subscribed;

            /// <summary>
            /// Gets the current snapshot of the registrations.
            /// </summary>
            public ErrorRegistration[] Registrations => Volatile.Read(ref this.registrations);

            /// <summary>
            /// 登记一条记录（复制整个数组后用 CAS 替换；条目数超过阈值时顺带丢掉 Tab 已被回收的失效条目）。
            /// </summary>
            /// <param name="registration">要登记的记录。</param>
            public void Add(ErrorRegistration registration)
            {
                while (true)
                {
                    var current = Volatile.Read(ref this.registrations);
                    ErrorRegistration[] next;
                    if (current.Length >= Volatile.Read(ref this.pruneThreshold))
                    {
                        var alive = new List<ErrorRegistration>(current.Length + 1);
                        foreach (var existing in current)
                        {
                            if (existing.TabReference.IsAlive)
                            {
                                alive.Add(existing);
                            }
                        }

                        alive.Add(registration);
                        next = alive.ToArray();
                        Volatile.Write(ref this.pruneThreshold, Math.Max(InitialRegistrationPruneThreshold, next.Length * 2));
                    }
                    else
                    {
                        next = new ErrorRegistration[current.Length + 1];
                        Array.Copy(current, next, current.Length);
                        next[current.Length] = registration;
                    }

                    if (ReferenceEquals(Interlocked.CompareExchange(ref this.registrations, next, current), current))
                    {
                        return;
                    }
                }
            }

            /// <summary>
            /// 标记已订阅。
            /// </summary>
            /// <returns>第一次调用返回 <c>true</c>（调用方负责订阅事件），之后都返回 <c>false</c>。</returns>
            public bool TryMarkSubscribed() => Interlocked.Exchange(ref this.subscribed, 1) == 0;
        }

        /// <summary>
        /// 记录 (Tab, 属性名, 错误控件) 三元组，Tab 与错误控件使用弱引用，避免订阅持有整个属性网格。
        /// </summary>
        private sealed class ErrorRegistration
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ErrorRegistration" /> class.
            /// </summary>
            /// <param name="tab">所属 Tab。</param>
            /// <param name="propertyName">属性名。</param>
            /// <param name="errorControl">该属性行的错误控件。</param>
            public ErrorRegistration(Tab tab, string propertyName, ContentControl errorControl)
            {
                this.TabReference = new WeakReference(tab);
                this.PropertyName = propertyName;
                this.ErrorControlReference = new WeakReference(errorControl);
                this.Dispatcher = errorControl.Dispatcher;
            }

            /// <summary>
            /// Gets the dispatcher of the UI thread that owns the error control (and the tab).
            /// </summary>
            public Dispatcher Dispatcher { get; }

            /// <summary>
            /// Gets the weak reference to the tab.
            /// </summary>
            public WeakReference TabReference { get; }

            /// <summary>
            /// Gets the property name.
            /// </summary>
            public string PropertyName { get; }

            /// <summary>
            /// Gets the weak reference to the error control of the property row.
            /// </summary>
            public WeakReference ErrorControlReference { get; }
        }

        /// <summary>
        /// Gets or sets the list of converters.
        /// </summary>
        /// <value>The converters.</value>
        public List<PropertyConverter> Converters { get; set; }

        /// <summary>
        /// Gets or sets the list of type editors.
        /// </summary>
        /// <value>The editors.</value>
        public List<TypeEditor> Editors { get; set; }

        /// <summary>
        /// Gets or sets the file dialog service.
        /// </summary>
        /// <value>The file dialog service.</value>
        public IFileDialogService FileDialogService { get; set; }

        /// <summary>
        /// Gets or sets the folder browser dialog service.
        /// </summary>
        /// <value>The folder browser dialog service.</value>
        public IFolderBrowserDialogService FolderBrowserDialogService { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use the DatePicker control for DateTime values.
        /// </summary>
        public bool UseDatePicker { get; set; }

        /// <summary>
        /// Creates the control for a property.
        /// </summary>
        /// <param name="property">The property item.</param>
        /// <param name="options">The options.</param>
        /// <returns>
        /// A element.
        /// </returns>
        public virtual FrameworkElement CreateControl(PropertyItem property, PropertyControlFactoryOptions options)
        {
            this.UpdateConverter(property);

            foreach (var editor in this.Editors)
            {
                if (editor.IsAssignable(property.Descriptor.PropertyType))
                {
                    return this.CreateEditorControl(property, editor);
                }
            }

            if (property.Is(typeof(bool)))
            {
                return this.CreateBoolControl(property);
            }

            if (property.Is(typeof(Enum)))
            {
                return this.CreateEnumControl(property, options);
            }

            if (property.Is(typeof(Color)))
            {
                return this.CreateColorControl(property);
            }

            if (property.Is(typeof(Brush)))
            {
                return this.CreateBrushControl(property);
            }

            if (property.Is(typeof(FontFamily)) || property.IsFontFamilySelector)
            {
                return this.CreateFontFamilyControl(property);
            }

            if (property.Is(typeof(ImageSource)) || property.DataTypes.Contains(DataType.ImageUrl))
            {
                return this.CreateImageControl(property);
            }

            if (property.Is(typeof(ICommand)))
            {
                return this.CreateCommandControl(property);
            }

            if (property.DataTypes.Contains(DataType.Html))
            {
                return this.CreateHtmlControl(property);
            }

            if (property.Is(typeof(Uri)))
            {
                return this.CreateLinkControl(property);
            }

            if (property.ItemsSourceDescriptor != null || property.ItemsSource != null)
            {
                return this.CreateComboBoxControl(property);
            }

            if (property.Is(typeof(SecureString)))
            {
                return this.CreateSecurePasswordControl(property);
            }

            if (this.UseDatePicker && property.Is(typeof(DateTime)))
            {
                return this.CreateDateTimeControl(property);
            }

            if (property.IsFilePath)
            {
                return this.CreateFilePathControl(property);
            }

            if (property.IsDirectoryPath)
            {
                return this.CreateDirectoryPathControl(property);
            }

            if (property.PreviewFonts)
            {
                return this.CreateFontPreview(property);
            }

            if (property.IsComment)
            {
                return this.CreateCommentControl(property);
            }

            if (property.IsContent)
            {
                return this.CreateContentControl(property);
            }

            if (property.IsPassword)
            {
                return this.CreatePasswordControl(property);
            }

            if (property.IsProgress)
            {
                return this.CreateProgressControl(property);
            }

            if (property.IsSlidable)
            {
                return this.CreateSliderControl(property);
            }

            if (property.IsSpinnable)
            {
                return this.CreateSpinControl(property);
            }

            if (property.CheckableItemsIsCheckedPropertyName != null)
            {
                return this.CreateCheckableItems(property);
            }

            if (property.Is(typeof(IDictionary)) || property.Is(typeof(IDictionary<,>)))
            {
                return this.CreateDictionaryControl(property);
            }

            if (property.Is(typeof(ICollection)) || property.Is(typeof(ICollection<>)))
            {
                return this.CreateGridControl(property);
            }

            return this.CreateDefaultControl(property);
        }

        /// <summary>
        /// Creates the error control.
        /// </summary>
        public virtual ContentControl CreateErrorControl(PropertyItem pi, object instance, Tab tab, PropertyControlFactoryOptions options)
        {
            var dataErrorInfoInstance = instance as IDataErrorInfo;
            var notifyDataErrorInfoInstance = instance as INotifyDataErrorInfo;

            var errorControl = new ContentControl
            {
                ContentTemplate = options.ValidationErrorTemplate,
                Focusable = false
            };
            IValueConverter errorConverter;
            string propertyPath;
            object source = null;
            if (dataErrorInfoInstance != null)
            {
                errorConverter = new DataErrorInfoConverter(dataErrorInfoInstance, pi.PropertyName);
                propertyPath = pi.PropertyName;
                source = instance;
            }
            else
            {
                errorConverter = new NotifyDataErrorInfoConverter(notifyDataErrorInfoInstance, pi.PropertyName);
                propertyPath = nameof(tab.HasErrors);
                source = tab;
                // 弱引用注册 + 每实例只订阅一次，避免每行订阅且永不退订导致的泄漏
                SubscribeErrorsChanged(notifyDataErrorInfoInstance, tab, pi.PropertyName, errorControl);
            }

            var visibilityBinding = new Binding(propertyPath)
            {
                Converter = errorConverter,
                NotifyOnTargetUpdated = true,
#if !NET40
                ValidatesOnNotifyDataErrors = false,
#endif
                Source = source,
            };

            var contentBinding = new Binding(propertyPath)
            {
                Converter = errorConverter,
#if !NET40
                ValidatesOnNotifyDataErrors = false,
#endif
                Source = source,
            };

            errorControl.SetBinding(UIElement.VisibilityProperty, visibilityBinding);

            // When the visibility of the error control is changed, updated the HasErrors of the tab
            errorControl.TargetUpdated += (s, e) =>
            {
                if (dataErrorInfoInstance != null)
                    tab.UpdateHasErrors(dataErrorInfoInstance, pi.PropertyName);
            };
            errorControl.SetBinding(ContentControl.ContentProperty, contentBinding);
            return errorControl;
        }

        /// <summary>
        /// sets error style for control
        /// </summary>
        /// <param name="control"></param>
        /// <param name="options"></param>
        public virtual void SetValidationErrorStyle(FrameworkElement control, PropertyControlFactoryOptions options)
        {
            // Only apply ValidationErrorStyle if the control doesn't already have a style applied.
            // This allows implicit styles from Style.Resources to take precedence.
            if (options.ValidationErrorStyle != null && control.Style == null)
            {
                control.Style = options.ValidationErrorStyle;
            }
            //return control;
        }

        /// <summary>
        /// Updates the tab for validation results.
        /// </summary>
        /// <param name="tab">The tab.</param>
        /// <param name="errorInfo">The error information.</param>
        public virtual void UpdateTabForValidationResults(Tab tab, object errorInfo)
        {
            if (errorInfo is INotifyDataErrorInfo ndei)
            {
                // 逐属性判断（与 Tab.UpdateHasErrors 的语义一致）：对象上别的 Tab 里的属性出错，不应让本 Tab 显示错误标记
                tab.HasErrors = tab.Groups.Any(g => g.Properties.Any(p => Tab.HasPropertyErrors(ndei, p.PropertyName)));
            }
            else if (errorInfo is IDataErrorInfo dei)
            {
                tab.HasErrors = tab.Groups.Any(g => g.Properties.Any(p => !string.IsNullOrEmpty(dei[p.PropertyName])));
            }
        }

        /// <summary>
        /// Converts the horizontal alignment.
        /// </summary>
        /// <param name="a">The alignment to convert.</param>
        /// <returns>
        /// A <see cref="HorizontalAlignment" />.
        /// </returns>
        protected static HorizontalAlignment ConvertHorizontalAlignment(core.DataAnnotations.HorizontalAlignment a)
        {
            switch (a)
            {
                case core.DataAnnotations.HorizontalAlignment.Center:
                    return HorizontalAlignment.Center;
                case core.DataAnnotations.HorizontalAlignment.Right:
                    return HorizontalAlignment.Right;
                default:
                    return HorizontalAlignment.Left;
            }
        }

        /// <summary>
        /// Creates the checkbox control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateBoolControl(PropertyItem property)
        {
            if (property.Descriptor.IsReadOnly())
            {
                var cm = new CheckMark
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                cm.SetBinding(CheckMark.IsCheckedProperty, property.CreateBinding());
                return cm;
            }

            var c = new CheckBox
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            c.SetBinding(ToggleButton.IsCheckedProperty, property.CreateBinding());
            return c;
        }

        /// <summary>
        /// Creates the command control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateCommandControl(PropertyItem property)
        {
            var button = new Button
            {
                Content = "Execute",
                Width = 100,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            button.SetBinding(Button.CommandProperty, property.CreateOneWayBinding());
            return button;
        }

        /// <summary>
        /// Creates a control based on a template from a a <see cref="TypeEditor" />.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="editor">The editor.</param>
        /// <returns>
        /// A <see cref="ContentControl" />.
        /// </returns>
        protected virtual ContentControl CreateEditorControl(PropertyItem property, TypeEditor editor)
        {
            var c = new ContentControl
            {
                ContentTemplate = editor.EditorTemplate,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            c.SetBinding(FrameworkElement.DataContextProperty, property.CreateOneWayBinding());
            return c;
        }

        /// <summary>
        /// Creates the brush control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateBrushControl(PropertyItem property)
        {
            var c = new ColorPicker();
            c.SetBinding(ColorPicker.SelectedColorProperty, property.CreateBinding());
            return c;
        }

        /// <summary>
        /// Creates the color control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateColorControl(PropertyItem property)
        {
            var c = new ColorPicker();
            c.SetBinding(ColorPicker.SelectedColorProperty, property.CreateBinding());
            return c;
        }

        /// <summary>
        /// Creates the combo box control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateComboBoxControl(PropertyItem property)
        {
            var c = new ComboBox { IsEditable = property.IsEditable, ItemsSource = property.ItemsSource, VerticalContentAlignment = VerticalAlignment.Center };
            if (property.ItemsSourceDescriptor != null)
            {
                c.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(property.ItemsSourceDescriptor.Name));
            }

            c.SelectedValuePath = property.SelectedValuePath;
            c.DisplayMemberPath = property.DisplayMemberPath;

            c.SetBinding(property.IsEditable ? ComboBox.TextProperty : Selector.SelectedValueProperty, property.CreateBinding());

            return c;
        }

        /// <summary>
        /// Creates a comment control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateCommentControl(PropertyItem property)
        {
            var tb = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4),
                Focusable = false,
                TextWrapping = TextWrapping.Wrap
            };
            ScrollViewer.SetHorizontalScrollBarVisibility(tb, ScrollBarVisibility.Hidden);
            ScrollViewer.SetVerticalScrollBarVisibility(tb, ScrollBarVisibility.Hidden);
            tb.SetBinding(TextBlock.TextProperty, property.CreateBinding());
            return tb;
        }

        /// <summary>
        /// Creates a content control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// A <see cref="ContentControl" />.
        /// </returns>
        protected virtual FrameworkElement CreateContentControl(PropertyItem property)
        {
            var b = new ContentControl
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4),
                Focusable = false,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };
            b.SetBinding(ContentControl.ContentProperty, property.CreateBinding());
            return b;
        }

        /// <summary>
        /// Creates the date time control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateDateTimeControl(PropertyItem property)
        {
            var c = new DatePicker();
            c.SetBinding(DatePicker.SelectedDateProperty, property.CreateBinding());
            return c;
        }

        /// <summary>
        /// Creates the default control (text box).
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateDefaultControl(PropertyItem property)
        {
            var trigger = property.AutoUpdateText ? UpdateSourceTrigger.PropertyChanged : UpdateSourceTrigger.Default;
            var c = new TextBoxEx
            {
                AcceptsReturn = property.AcceptsReturn,
                MaxLength = property.MaxLength,
                IsReadOnly = property.IsReadOnly,
                TextWrapping = property.TextWrapping,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalContentAlignment = property.AcceptsReturn ? VerticalAlignment.Top : VerticalAlignment.Center
            };

            if (property.FontFamily != null)
            {
                c.FontFamily = new FontFamily(property.FontFamily);
            }

            if (!double.IsNaN(property.FontSize))
            {
                c.FontSize = property.FontSize;
            }

            if (property.IsReadOnly)
            {
                c.Foreground = Brushes.RoyalBlue;
            }

            var binding = property.CreateBinding(trigger);
            if (property.ActualPropertyType != typeof(string) && IsNullable(property.ActualPropertyType))
            {
                // Empty values should set the source to null
                // Set the value that is used in the target when the value of the source is null.
                binding.TargetNullValue = string.Empty;
            }

            c.SetBinding(TextBox.TextProperty, binding);

            return c;
        }

        /// <summary>
        /// Creates a dictionary control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateDictionaryControl(PropertyItem property)
        {
            var c = new ComboBox();
            c.SetBinding(ItemsControl.ItemsSourceProperty, property.CreateBinding());
            return c;
        }

        /// <summary>
        /// Creates the directory path control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateDirectoryPathControl(PropertyItem property)
        {
            var c = new DirectoryPicker { FolderBrowserDialogService = this.FolderBrowserDialogService };
            var trigger = property.AutoUpdateText ? UpdateSourceTrigger.PropertyChanged : UpdateSourceTrigger.Default;
            c.SetBinding(DirectoryPicker.DirectoryProperty, property.CreateBinding(trigger));
            return c;
        }

        /// <summary>
        /// Gets the values for the specified enumeration type.
        /// </summary>
        /// <param name="enumType">The enumeration type.</param>
        /// <returns>A sequence of values.</returns>
        protected virtual IEnumerable<object> GetEnumValues(Type enumType)
        {
            var ult = Nullable.GetUnderlyingType(enumType);
            var isNullable = ult != null;
            if (isNullable)
            {
                enumType = ult;
            }

            var enumValues = Enum.GetValues(enumType).FilterOnBrowsableAttribute().ToList();
            if (isNullable)
            {
                enumValues.Add(null);
            }

            return enumValues;
        }

        /// <summary>
        /// Creates the select control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="options">The options.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateEnumControl(
            PropertyItem property, PropertyControlFactoryOptions options)
        {
            //// var isBitField = property.Descriptor.PropertyType.GetTypeInfo().GetCustomAttributes<FlagsAttribute>().Any();

            var values = this.GetEnumValues(property.Descriptor.PropertyType).ToArray();
            var style = property.SelectorStyle;
            if (style == core.DataAnnotations.SelectorStyle.Auto)
            {
                style = values.Length > options.EnumAsRadioButtonsLimit
                            ? core.DataAnnotations.SelectorStyle.ComboBox
                            : core.DataAnnotations.SelectorStyle.RadioButtons;
            }

            switch (style)
            {
                case core.DataAnnotations.SelectorStyle.RadioButtons:
                    {
                        var c = new RadioButtonList { EnumType = property.Descriptor.PropertyType };
                        c.SetBinding(RadioButtonList.ValueProperty, property.CreateBinding());
                        return c;
                    }

                case core.DataAnnotations.SelectorStyle.ComboBox:
                    {
                        var c = new ComboBox { ItemsSource = values };
                        c.SetBinding(Selector.SelectedValueProperty, property.CreateBinding());
                        return c;
                    }

                case core.DataAnnotations.SelectorStyle.ListBox:
                    {
                        var c = new ListBox { ItemsSource = values };
                        c.SetBinding(Selector.SelectedValueProperty, property.CreateBinding());
                        return c;
                    }

                default:
                    return null;
            }
        }

        /// <summary>
        /// Creates the file path control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateFilePathControl(PropertyItem property)
        {
            var c = new FilePicker
            {
                Filter = property.FilePathFilter,
                DefaultExtension = property.FilePathDefaultExtension,
                UseOpenDialog = property.IsFileOpenDialog,
                FileDialogService = this.FileDialogService
            };

            if (property.RelativePathDescriptor != null)
            {
                c.SetBinding(FilePicker.BasePathProperty, new Binding(property.RelativePathDescriptor.Name));
            }

            if (property.FilterDescriptor != null)
            {
                c.SetBinding(FilePicker.FilterProperty, new Binding(property.FilterDescriptor.Name));
            }

            if (property.DefaultExtensionDescriptor != null)
            {
                c.SetBinding(FilePicker.DefaultExtensionProperty, new Binding(property.DefaultExtensionDescriptor.Name));
            }

            var trigger = property.AutoUpdateText ? UpdateSourceTrigger.PropertyChanged : UpdateSourceTrigger.Default;
            c.SetBinding(FilePicker.FilePathProperty, property.CreateBinding(trigger));
            return c;
        }

        /// <summary>
        /// Creates the font family control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateFontFamilyControl(PropertyItem property)
        {
            var c = new ComboBox { ItemsSource = GetFontFamilies() };

            if (property.PreviewFonts)
            {
                var dt = new DataTemplate { DataType = typeof(ComboBox) };
                var factory = new FrameworkElementFactory(typeof(TextBlock));
                factory.SetValue(TextBlock.FontSizeProperty, property.FontSize);
                factory.SetValue(TextBlock.FontWeightProperty, FontWeight.FromOpenTypeWeight(property.FontWeight));
                factory.SetBinding(TextBlock.TextProperty, new Binding());
                factory.SetBinding(TextBlock.FontFamilyProperty, new Binding { Converter = FontFamilyConverter });
                dt.VisualTree = factory;
                c.ItemTemplate = dt;
            }

            var binding = property.CreateBinding();
            if (property.ActualPropertyType == typeof(string))
            {
                binding.Converter = FontFamilyConverter;
            }

            c.SetBinding(Selector.SelectedValueProperty, binding);
            return c;
        }

        /// <summary>
        /// Creates the font preview.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateFontPreview(PropertyItem property)
        {
            var c = new TextBoxEx
            {
                Background = Brushes.Transparent,
                BorderBrush = null,
                AcceptsReturn = true,
                IsReadOnly = property.IsReadOnly,
                TextWrapping = TextWrapping.Wrap,
                FontWeight = FontWeight.FromOpenTypeWeight(property.FontWeight),
                FontSize = property.FontSize
            };
            TextOptions.SetTextFormattingMode(c, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(c, TextRenderingMode.ClearType);
            c.SetBinding(TextBox.TextProperty, new Binding(property.Descriptor.Name) { Mode = BindingMode.OneWay });
            if (property.FontFamilyPropertyDescriptor != null)
            {
                c.SetBinding(Control.FontFamilyProperty, new Binding(property.FontFamilyPropertyDescriptor.Name));
            }

            return c;
        }

        /// <summary>
        /// Creates the grid control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateGridControl(PropertyItem property)
        {
            var c = new DataGrid
            {
                CanDelete = property.ListCanRemove,
                CanInsert = property.ListCanAdd,
                InputDirection = property.InputDirection,
                IsEasyInsertByMouseEnabled = property.IsEasyInsertByMouseEnabled,
                IsEasyInsertByKeyboardEnabled = property.IsEasyInsertByKeyboardEnabled,
                AutoGenerateColumns = property.Columns.Count == 0
            };

            foreach (var cd in property.Columns)
            {
                if (cd.PropertyName == string.Empty && property.ListItemItemsSource != null)
                {
                    cd.ItemsSource = property.ListItemItemsSource;
                }

                c.ColumnDefinitions.Add(cd);
            }

            c.SetBinding(DataGrid.ItemsSourceProperty, property.CreateBinding());
            return c;
        }

        /// <summary>
        /// Creates the HTML control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateHtmlControl(PropertyItem property)
        {
            var c = new WebBrowser();
            c.SetBinding(WebBrowserBehavior.NavigateToStringProperty, property.CreateBinding());
            return c;
        }

        /// <summary>
        /// Creates the image control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateImageControl(PropertyItem property)
        {
            var c = new Image { Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Left };
            c.SetBinding(Image.SourceProperty, property.CreateOneWayBinding());
            return c;
        }

        /// <summary>
        /// Creates the link control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateLinkControl(PropertyItem property)
        {
            var c = new LinkBlock { VerticalAlignment = VerticalAlignment.Center };
            c.SetBinding(TextBlock.TextProperty, new Binding(property.Descriptor.Name));
            c.SetBinding(LinkBlock.NavigateUriProperty, property.CreateBinding());
            return c;
        }

        /// <summary>
        /// Creates the password control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreatePasswordControl(PropertyItem property)
        {
            var c = new PasswordBox();
            PasswordHelper.SetAttach(c, true);
            c.SetBinding(PasswordHelper.PasswordProperty, property.CreateBinding());
            return c;
        }

        /// <summary>
        /// Creates the secure password control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateSecurePasswordControl(PropertyItem property)
        {
            var c = new PasswordBox();

            // PasswordHelper.SetAttach(b, true);
            // b.SetBinding(PasswordHelper.PasswordProperty, pi.CreateBinding());

            return c;
        }

        /// <summary>
        /// Creates the progress control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateProgressControl(PropertyItem property)
        {
            var c = new ProgressBar
            {
                Minimum = property.ProgressMinimum,
                Maximum = property.ProgressMaximum,
                Margin = new Thickness(2)
            };
            c.SetBinding(RangeBase.ValueProperty, property.CreateBinding());
            return c;
        }

        /// <summary>
        /// Creates the slider control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateSliderControl(PropertyItem property)
        {
            var g = new Grid();
            g.ColumnDefinitions.Add(
                new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(
                new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var s = new Slider
            {
                Minimum = property.SliderMinimum,
                Maximum = property.SliderMaximum,
                SmallChange = property.SliderSmallChange,
                LargeChange = property.SliderLargeChange,
                TickFrequency = property.SliderTickFrequency,
                IsSnapToTickEnabled = property.SliderSnapToTicks
            };
            s.SetBinding(RangeBase.ValueProperty, property.CreateBinding());
            g.Children.Add(s);

            var trigger = property.AutoUpdateText ? UpdateSourceTrigger.PropertyChanged : UpdateSourceTrigger.Default;
            var c = new TextBoxEx { IsReadOnly = property.Descriptor.IsReadOnly };

            var formatString = property.FormatString;
            if (formatString != null && !formatString.StartsWith("{"))
            {
                formatString = "{0:" + formatString + "}";
            }

            var binding = property.CreateBinding();
            binding.StringFormat = formatString;
            binding.UpdateSourceTrigger = trigger;

            c.SetBinding(TextBox.TextProperty, binding);

            Grid.SetColumn(c, 1);
            g.Children.Add(c);

            return g;
        }

        /// <summary>
        /// Creates the spin control.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The control.
        /// </returns>
        protected virtual FrameworkElement CreateSpinControl(PropertyItem property)
        {
            var tb = new TextBoxEx
            {
                IsReadOnly = property.IsReadOnly,
                BorderThickness = new Thickness(0),
                HorizontalContentAlignment = ConvertHorizontalAlignment(property.HorizontalAlignment),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            tb.SetBinding(TextBox.TextProperty, property.CreateBinding());
            var c = new SpinControl
            {
                Maximum = property.SpinMaximum,
                Minimum = property.SpinMinimum,
                SmallChange = property.SpinSmallChange,
                LargeChange = property.SpinLargeChange,
                Content = tb
            };

            // Note: Do not apply the converter to the SpinControl
            c.SetBinding(SpinControl.ValueProperty, property.CreateBinding(UpdateSourceTrigger.Default, false));
            return c;
        }

        /// <summary>
        /// Creates a sequence of checkboxes.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// A FrameworkElement.
        /// </returns>
        protected virtual FrameworkElement CreateCheckableItems(PropertyItem property)
        {
            var lb = new ItemsControl();
            var rectangleFactory = new FrameworkElementFactory(typeof(CheckBox));
            rectangleFactory.SetBinding(ToggleButton.IsCheckedProperty, new Binding(property.CheckableItemsIsCheckedPropertyName));
            rectangleFactory.SetBinding(ContentControl.ContentProperty, new Binding(property.CheckableItemsContentPropertyName));

            lb.ItemTemplate = new DataTemplate { VisualTree = rectangleFactory };
            lb.SetBinding(ItemsControl.ItemsSourceProperty, property.CreateBinding());
            lb.Margin = new Thickness(0, 6, 0, 6);

            return lb;
        }

        /// <summary>
        /// Updates the converter from the Converters collection.
        /// </summary>
        /// <param name="property">The property.</param>
        protected void UpdateConverter(PropertyItem property)
        {
            if (property.Converter == null)
            {
                foreach (var c in this.Converters)
                {
                    if (c.IsAssignable(property.Descriptor.PropertyType))
                    {
                        property.Converter = c.Converter;
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the specified type is a reference type or a <see cref="Nullable" />.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// <c>true</c> if the specified type can be set to <c>null</c>; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsNullable(Type type)
        {
            if (!type.IsValueType)
            {
                // Reference types are nullable
                return true;
            }

            if (Nullable.GetUnderlyingType(type) != null)
            {
                // Nullable value type
                return true;
            }

            // Value type
            return false;
        }

        /// <summary>
        /// Gets the font families.
        /// </summary>
        /// <returns>
        /// List of font families.
        /// </returns>
        private static IEnumerable<FontFamily> GetFontFamilies()
        {
            if (cachedFontFamilies == null)
            {
                var list = new List<FontFamily>();
                foreach (var fontFamily in Fonts.SystemFontFamilies)
                {
                    // Instantiate a TypeFace object with the font settings you want to use
                    var ltypFace = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

                    try
                    {
                        GlyphTypeface gtf;
                        if (ltypFace.TryGetGlyphTypeface(out gtf))
                        {
                            list.Add(fontFamily);
                        }
                    }
                    catch (FileFormatException)
                    {
                        Debug.WriteLine(fontFamily + " failed.");
                    }
                }

                cachedFontFamilies = list.OrderBy(f => f.ToString()).ToArray();
            }

            return cachedFontFamilies;
        }
    }
}