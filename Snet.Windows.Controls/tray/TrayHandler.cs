// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Snet.Windows.Controls.tray;

/// <summary>
/// Win32 API 和 Windows 消息管理器。<br/>
/// 通过创建一个透明的子窗口来接收系统托盘的 Windows 消息。
/// </summary>
internal class TrayHandler : HwndSource
{
    /// <summary>
    /// 获取或设置挂钩元素的标识 ID。
    /// </summary>
    public int ElementId { get; internal set; }

    /// <summary>
    /// 初始化 <see cref="TrayHandler"/> 类的新实例。<br/>
    /// 创建一个带透明参数、无大小、默认位置的子窗口，并附加默认的消息委托。
    /// </summary>
    /// <param name="name">创建的窗口名称。</param>
    /// <param name="parent">父窗口句柄。</param>
    public TrayHandler(string name, IntPtr parent)
        : base(0x0, 0x4000000, 0x80000 | 0x20 | 0x00000008 | 0x08000000, 0, 0, 0, 0, name, parent)
    {
        System.Diagnostics.Debug.WriteLine(
            $"INFO | New {typeof(TrayHandler)} registered with handle: #{Handle}, and parent: #{parent}",
            "Wpf.Ui.TrayHandler"
        );
    }
}
