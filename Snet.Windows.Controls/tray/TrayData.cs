// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Snet.Windows.Controls.tray;

/// <summary>
/// 托盘图标数据单例容器。<br/>
/// 在应用程序会话期间保存已注册的托盘图标信息。
/// </summary>
internal static class TrayData
{
    /// <summary>
    /// 获取或设置已注册的托盘图标集合。
    /// </summary>
    public static List<INotifyIcon> NotifyIcons { get; set; } = new();

    /// <summary>
    /// 从已注册集合中移除指定托盘图标，使其 Id 可被复用。
    /// </summary>
    /// <param name="notifyIcon">要移除的托盘图标实例。</param>
    public static void Remove(INotifyIcon notifyIcon)
    {
        NotifyIcons.Remove(notifyIcon);
    }

    /// <summary>
    /// 分配最小的未使用 Id（注销后的 Id 会被复用）。
    /// </summary>
    /// <returns>可用的托盘图标 Id。</returns>
    public static int AllocateId()
    {
        int id = 1;
        while (NotifyIcons.Exists(n => n.Id == id))
        {
            id++;
        }
        return id;
    }
}
