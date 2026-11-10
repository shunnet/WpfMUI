// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

#pragma warning disable CA1001 // Types that own disposable fields should be disposable

namespace Snet.Windows.Controls.tray;

/// <summary>
/// Base implementation of the notify icon service.
/// </summary>
public class NotifyIconService : INotifyIconService
{
    private readonly Internal.InternalNotifyIconManager internalNotifyIconManager;

    /// <summary>
    /// Gets the window that owns the tray icon (set by <see cref="SetParentWindow"/>).
    /// </summary>
    public Window ParentWindow { get; internal set; } = null!;

    /// <summary>
    /// Gets the identifier of the icon within the shell notification area.
    /// </summary>
    public int Id => internalNotifyIconManager.Id;

    /// <summary>
    /// Gets a value indicating whether the icon is currently registered in the notification area.
    /// </summary>
    public bool IsRegistered => internalNotifyIconManager.IsRegistered;

    /// <summary>
    /// Gets or sets the tooltip text; setting it refreshes the tray icon immediately.
    /// </summary>
    public string TooltipText
    {
        get => internalNotifyIconManager.TooltipText;
        set
        {
            internalNotifyIconManager.TooltipText = value;
            _ = internalNotifyIconManager.ModifyToolTip();
        }
    }

    /// <summary>
    /// Gets or sets the context menu shown for the tray icon.
    /// </summary>
    public ContextMenu? ContextMenu
    {
        get => internalNotifyIconManager.ContextMenu;
        set => internalNotifyIconManager.ContextMenu = value;
    }

    /// <summary>
    /// Gets or sets the image displayed in the notification area; setting it refreshes the tray icon immediately.
    /// </summary>
    public ImageSource? Icon
    {
        get => internalNotifyIconManager.Icon;
        set
        {
            internalNotifyIconManager.Icon = value;

            // 与 TooltipText 一致：设置后立即刷新托盘图标（NotifyIcon 控件的 OnIconChanged 也是这样做的）
            _ = internalNotifyIconManager.ModifyIcon();
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyIconService"/> class and hooks the icon's click events to the <c>On*Click</c> virtual methods.
    /// </summary>
    public NotifyIconService()
    {
        internalNotifyIconManager = new Internal.InternalNotifyIconManager();

        RegisterHandlers();
    }

    /// <summary>
    /// Registers the icon in the notification area (owned by <see cref="ParentWindow"/> when one was set).
    /// </summary>
    /// <returns><see langword="true"/> when the icon was registered.</returns>
    public bool Register()
    {
        if (ParentWindow is not null)
        {
            return internalNotifyIconManager.Register(ParentWindow);
        }

        return internalNotifyIconManager.Register();
    }

    /// <summary>
    /// Removes the icon from the notification area.
    /// </summary>
    /// <returns><see langword="true"/> when the icon was removed.</returns>
    public bool Unregister()
    {
        return internalNotifyIconManager.Unregister();
    }

    /// <inheritdoc />
    public void SetParentWindow(Window parentWindow)
    {
        if (ParentWindow is not null)
        {
            ParentWindow.Closed -= OnParentWindowClosed;
        }

        ParentWindow = parentWindow;

        // 必须订阅 Closed 而不是 Closing：Closing 可以被取消（"关闭到托盘"模式下 e.Cancel = true 再隐藏窗口），
        // 在 Closing 里释放会在窗口并没有真正关闭时就删除托盘图标并清空事件
        ParentWindow.Closed += OnParentWindowClosed;
    }

    /// <summary>
    /// This virtual method is called when the user clicks the left mouse button on the tray icon.
    /// </summary>
    protected virtual void OnLeftClick() { }

    /// <summary>
    /// This virtual method is called when the user double-clicks the left mouse button on the tray icon.
    /// </summary>
    protected virtual void OnLeftDoubleClick() { }

    /// <summary>
    /// This virtual method is called when the user clicks the right mouse button on the tray icon.
    /// </summary>
    protected virtual void OnRightClick() { }

    /// <summary>
    /// This virtual method is called when the user double-clicks the right mouse button on the tray icon.
    /// </summary>
    protected virtual void OnRightDoubleClick() { }

    /// <summary>
    /// This virtual method is called when the user clicks the middle mouse button on the tray icon.
    /// </summary>
    protected virtual void OnMiddleClick() { }

    /// <summary>
    /// This virtual method is called when the user double-clicks the middle mouse button on the tray icon.
    /// </summary>
    protected virtual void OnMiddleDoubleClick() { }

    private void OnParentWindowClosed(object? sender, EventArgs e)
    {
        internalNotifyIconManager.Dispose();
    }

    private void RegisterHandlers()
    {
        internalNotifyIconManager.LeftClick += OnLeftClick;
        internalNotifyIconManager.LeftDoubleClick += OnLeftDoubleClick;
        internalNotifyIconManager.RightClick += OnRightClick;
        internalNotifyIconManager.RightDoubleClick += OnRightDoubleClick;
        internalNotifyIconManager.MiddleClick += OnMiddleClick;
        internalNotifyIconManager.MiddleDoubleClick += OnMiddleDoubleClick;
    }
}

#pragma warning restore CA1001 // Types that own disposable fields should be disposable
