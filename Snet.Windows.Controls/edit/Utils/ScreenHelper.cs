using System.Windows;

namespace Snet.Windows.Controls.edit.Utils
{
    /// <summary>
    /// 获取指定屏幕坐标所在显示器的工作区，替代 System.Windows.Forms.Screen，
    /// 避免引入 WinForms 依赖。多显示器语义与 Screen.GetWorkingArea / Screen.FromPoint 一致。
    /// </summary>
    internal static class ScreenHelper
    {
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        /// <summary>
        /// 获取包含指定屏幕坐标（设备像素）的显示器工作区，返回设备像素 Rect。
        /// </summary>
        public static Rect GetWorkingArea(Point pointOnScreen)
        {
            var pt = new POINT { X = (int)Math.Round(pointOnScreen.X), Y = (int)Math.Round(pointOnScreen.Y) };
            IntPtr hMonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
            // DEFAULTTONEAREST 在有显示器时总返回有效句柄；此兜底仅覆盖无显示器等极端情况
            if (hMonitor == IntPtr.Zero)
                hMonitor = MonitorFromPoint(new POINT(), MONITOR_DEFAULTTONEAREST);

            var info = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
            if (GetMonitorInfo(hMonitor, ref info))
            {
                return new Rect(
                    info.rcWork.Left, info.rcWork.Top,
                    info.rcWork.Right - info.rcWork.Left,
                    info.rcWork.Bottom - info.rcWork.Top);
            }
            return Rect.Empty;
        }
    }
}
