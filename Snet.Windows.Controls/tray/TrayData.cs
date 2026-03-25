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
}
