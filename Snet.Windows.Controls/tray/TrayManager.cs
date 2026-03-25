// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows;

namespace Snet.Windows.Controls.tray;

/*
 * TODO: Handle closing of the parent window.
 * NOTE
 * The problem is as follows:
 * If the main window is closed with the Debugger or simply destroyed,
 * it will not send WM_CLOSE or WM_DESTROY to its child windows. This
 * way, we can't tell tray to close the icon. Thus, we need to add to
 * the TrayHandler a mechanism that detects that the parent window has
 * been closed and then send
 * Shell32.Shell_NotifyIcon(Shell32.NIM.DELETE, Shell32.NOTIFYICONDATA);
 *
 * In another situation, the TrayHandler can also be forced to close,
 * so there is need to detect from the side somehow if this has happened
 * and remove the icon.
 */

/// <summary>
/// 系统托盘图标管理器。<br/>
/// 负责注册、修改和注销系统托盘中的图标。
/// </summary>
internal static class TrayManager
{
    /// <summary>
    /// 使用默认父窗口注册托盘图标。
    /// </summary>
    /// <param name="notifyIcon">要注册的通知图标实例。</param>
    /// <returns>注册是否成功。</returns>
    public static bool Register(INotifyIcon notifyIcon)
    {
        if (notifyIcon is null)
        {
            return false;
        }

        return Register(notifyIcon, GetParentSource());
    }

    /// <summary>
    /// 使用指定父窗口注册托盘图标。
    /// </summary>
    /// <param name="notifyIcon">要注册的通知图标实例。</param>
    /// <param name="parentWindow">父窗口。</param>
    /// <returns>注册是否成功。</returns>
    public static bool Register(INotifyIcon notifyIcon, Window parentWindow)
    {
        if (parentWindow == null)
        {
            return false;
        }

        return Register(notifyIcon, (HwndSource)PresentationSource.FromVisual(parentWindow));
    }

    /// <summary>
    /// 使用指定的 HwndSource 注册托盘图标。<br/>
    /// 包括创建 TrayHandler、设置 Shell32 通知数据、加载图标和消息钩子。
    /// </summary>
    /// <param name="notifyIcon">要注册的通知图标实例。</param>
    /// <param name="parentSource">父窗口的 HwndSource，可为 null。</param>
    /// <returns>注册是否成功。</returns>
    public static bool Register(INotifyIcon notifyIcon, HwndSource? parentSource)
    {
        if (parentSource is null)
        {
            if (!notifyIcon.IsRegistered)
            {
                return false;
            }

            _ = Unregister(notifyIcon);

            return false;
        }

        if (parentSource.Handle == IntPtr.Zero)
        {
            return false;
        }

        if (notifyIcon.IsRegistered)
        {
            _ = Unregister(notifyIcon);
        }

        notifyIcon.Id = TrayData.NotifyIcons.Count + 1;

        notifyIcon.HookWindow = new TrayHandler(
            $"wpfui_th_{parentSource.Handle}_{notifyIcon.Id}",
            parentSource.Handle
        )
        {
            ElementId = notifyIcon.Id,
        };

        notifyIcon.ShellIconData = new Interop.Shell32.NOTIFYICONDATA
        {
            uID = notifyIcon.Id,
            uFlags = Interop.Shell32.NIF.MESSAGE,
            uCallbackMessage = (int)Interop.User32.WM.TRAYMOUSEMESSAGE,
            hWnd = notifyIcon.HookWindow.Handle,
            dwState = 0x2,
        };

        if (!string.IsNullOrEmpty(notifyIcon.TooltipText))
        {
            notifyIcon.ShellIconData.szTip = notifyIcon.TooltipText;
            notifyIcon.ShellIconData.uFlags |= Interop.Shell32.NIF.TIP;
        }

        ReloadHicon(notifyIcon);

        notifyIcon.HookWindow.AddHook(notifyIcon.WndProc);

        _ = Interop.Shell32.Shell_NotifyIcon(Interop.Shell32.NIM.ADD, notifyIcon.ShellIconData);

        TrayData.NotifyIcons.Add(notifyIcon);

        notifyIcon.IsRegistered = true;

        return true;
    }

    /// <summary>
    /// 修改托盘图标的显示图像。
    /// </summary>
    /// <param name="notifyIcon">要修改的通知图标实例。</param>
    /// <returns>修改是否成功。</returns>
    public static bool ModifyIcon(INotifyIcon notifyIcon)
    {
        if (!notifyIcon.IsRegistered)
        {
            return true;
        }

        ReloadHicon(notifyIcon);

        return Interop.Shell32.Shell_NotifyIcon(Interop.Shell32.NIM.MODIFY, notifyIcon.ShellIconData);
    }

    /// <summary>
    /// 修改托盘图标的提示文本。
    /// </summary>
    /// <param name="notifyIcon">要修改的通知图标实例。</param>
    /// <returns>修改是否成功。</returns>
    public static bool ModifyToolTip(INotifyIcon notifyIcon)
    {
        if (!notifyIcon.IsRegistered)
        {
            return true;
        }

        notifyIcon.ShellIconData.szTip = notifyIcon.TooltipText;
        notifyIcon.ShellIconData.uFlags |= Interop.Shell32.NIF.TIP;

        return Interop.Shell32.Shell_NotifyIcon(Interop.Shell32.NIM.MODIFY, notifyIcon.ShellIconData);
    }

    /// <summary>
    /// 尝试从 Shell 中注销 <see cref="INotifyIcon"/>，移除托盘图标。
    /// </summary>
    /// <param name="notifyIcon">要注销的通知图标实例。</param>
    /// <returns>注销是否成功。</returns>
    public static bool Unregister(INotifyIcon notifyIcon)
    {
        if (notifyIcon.ShellIconData == null || !notifyIcon.IsRegistered)
        {
            return false;
        }

        _ = Interop.Shell32.Shell_NotifyIcon(Interop.Shell32.NIM.DELETE, notifyIcon.ShellIconData);

        notifyIcon.IsRegistered = false;

        return true;
    }

    /// <summary>
    /// 获取应用程序主窗口的 HwndSource。
    /// </summary>
    /// <returns>主窗口的 HwndSource，若主窗口不存在则返回 null。</returns>
    private static HwndSource? GetParentSource()
    {
        Window mainWindow = Application.Current.MainWindow;

        if (mainWindow == null)
        {
            return null;
        }

        return (HwndSource)PresentationSource.FromVisual(mainWindow);
    }

    /// <summary>
    /// 重新加载托盘图标的 HICON 句柄。<br/>
    /// 优先使用自定义图标，若无则使用应用程序默认图标。
    /// </summary>
    /// <param name="notifyIcon">要加载图标的通知图标实例。</param>
    private static void ReloadHicon(INotifyIcon notifyIcon)
    {
        IntPtr hIcon = IntPtr.Zero;

        if (notifyIcon.Icon is not null)
        {
            hIcon = Hicon.FromSource(notifyIcon.Icon);
        }

        if (hIcon == IntPtr.Zero)
        {
            hIcon = Hicon.FromApp();
        }

        if (hIcon != IntPtr.Zero)
        {
            notifyIcon.ShellIconData.hIcon = hIcon;
            notifyIcon.ShellIconData.uFlags |= Interop.Shell32.NIF.ICON;
        }
    }
}
