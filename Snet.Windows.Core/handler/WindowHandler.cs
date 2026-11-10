using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace Snet.Windows.Core.handler;

/// <summary>
/// 提供对原生 Win32 窗口操作的封装，适用于：
/// - WPF 无边框窗口的最大化/最小化控制
/// - 自定义标题栏拖拽、调整大小
/// - DPI 适配与系统边框修正
/// </summary>
public static class WindowHandler
{
    #region 公共属性
    /// <summary>
    /// 配置文件基础路径
    /// </summary>
    public static readonly string BasePath = Path.Combine(AppContext.BaseDirectory, "config");
    #endregion

    #region 常量定义（Win32 消息与系统指标）
    /// <summary>
    /// 可调整大小的窗口边框的宽度（X 方向，像素），GetSystemMetrics 的 nIndex 取值
    /// </summary>
    public const int SM_CXFRAME = 32;
    /// <summary>
    /// 可调整大小的窗口边框的高度（Y 方向，像素），GetSystemMetrics 的 nIndex 取值
    /// </summary>
    public const int SM_CYFRAME = 33;
    /// <summary>
    /// 带标题窗口的边框附加填充宽度（像素），与 SM_CXFRAME 相加得到完整的边框厚度
    /// </summary>
    public const int SM_CXPADDEDBORDER = 92;

    /// <summary>
    /// SetWindowPos 标志：保持当前 Z 序（忽略 hWndInsertAfter 参数）
    /// </summary>
    public const uint SWP_NOZORDER = 0x0004;
    /// <summary>
    /// SetWindowPos 标志：不激活窗口
    /// </summary>
    public const uint SWP_NOACTIVATE = 0x0010;

    /// <summary>
    /// MonitorFromWindow / MonitorFromPoint 标志：目标不在任何显示器上时，返回距离最近的显示器
    /// </summary>
    public const int MONITOR_DEFAULTTONEAREST = 0x0002;

    /// <summary>
    /// 窗口消息：窗口即将改变大小或位置时发送，lParam 指向 MINMAXINFO，可用来限制最大化尺寸与最小/最大跟踪尺寸
    /// </summary>
    public const int WM_GETMINMAXINFO = 0x0024;
    /// <summary>
    /// 窗口消息：窗口被移动之后发送
    /// </summary>
    public const int WM_MOVE = 0x0003;
    /// <summary>
    /// 窗口消息：鼠标命中测试，确定光标位于窗口的哪个区域（返回值见 HitTestResult）
    /// </summary>
    public const int WM_NCHITTEST = 0x0084;
    /// <summary>
    /// 窗口消息：用户拖动边框调整窗口大小的过程中持续发送
    /// </summary>
    public const int WM_SIZING = 0x0214;

    /// <summary>
    /// GetDpiForMonitor 的 dpiType 取值：有效 DPI（已包含用户的缩放设置）
    /// </summary>
    public const int MDT_EFFECTIVE_DPI = 0;

    // DWM 属性
    /// <summary>
    /// DWM 窗口属性：窗口圆角偏好（Windows 11）
    /// </summary>
    public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    /// <summary>
    /// DWM 窗口属性：窗口扩展边框的屏幕矩形（不含阴影）
    /// </summary>
    public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
    /// <summary>
    /// DWM 窗口属性：标题栏颜色（COLORREF，Windows 11 22H2 及以上，低版本忽略）
    /// </summary>
    public const int DWMWA_CAPTION_COLOR = 35;
    /// <summary>
    /// DWM 窗口属性：窗口边框颜色（COLORREF，Windows 11 22H2 及以上，低版本忽略）
    /// </summary>
    public const int DWMWA_BORDER_COLOR = 34;
    /// <summary>
    /// DWM 窗口属性：窗口可见边框的厚度（像素，Windows 11）
    /// </summary>
    public const int DWMWA_VISIBLE_FRAME_BORDER_THICKNESS = 37;

    // 窗口类属性
    /// <summary>
    /// GetClassLongPtr / SetClassLongPtr 的 nIndex 取值：窗口类的背景画刷句柄
    /// </summary>
    public const int GCLP_HBRBACKGROUND = -10;
    #endregion

    #region 枚举定义

    /// <summary>
    /// 鼠标命中测试结果，决定鼠标所在窗口区域
    /// </summary>
    public enum HitTestResult
    {
        /// <summary>
        /// 屏幕背景或窗口之间的分隔线（与 HTNOWHERE 相同，但系统会发出错误提示音）
        /// </summary>
        HTERROR = -2,
        /// <summary>
        /// 同一线程中被其它窗口覆盖的窗口区域（命中测试继续向下层窗口传递）
        /// </summary>
        HTTRANSPARENT = -1,
        /// <summary>
        /// 屏幕背景或窗口之间的分隔线
        /// </summary>
        HTNOWHERE = 0,
        /// <summary>
        /// 客户区
        /// </summary>
        HTCLIENT = 1,
        /// <summary>
        /// 标题栏
        /// </summary>
        HTCAPTION = 2,
        /// <summary>
        /// 窗口菜单，或子窗口的关闭按钮
        /// </summary>
        HTSYSMENU = 3,
        /// <summary>
        /// 大小调整框（与 HTSIZE 相同）
        /// </summary>
        HTGROWBOX = 4,
        /// <summary>
        /// 大小调整框（同 HTGROWBOX）
        /// </summary>
        HTSIZE = HTGROWBOX,
        /// <summary>
        /// 菜单栏
        /// </summary>
        HTMENU = 5,
        /// <summary>
        /// 水平滚动条
        /// </summary>
        HTHSCROLL = 6,
        /// <summary>
        /// 垂直滚动条
        /// </summary>
        HTVSCROLL = 7,
        /// <summary>
        /// 最小化按钮
        /// </summary>
        HTMINBUTTON = 8,
        /// <summary>
        /// 最大化按钮
        /// </summary>
        HTMAXBUTTON = 9,
        /// <summary>
        /// 可调整大小窗口的左边框
        /// </summary>
        HTLEFT = 10,
        /// <summary>
        /// 可调整大小窗口的右边框
        /// </summary>
        HTRIGHT = 11,
        /// <summary>
        /// 可调整大小窗口的上边框
        /// </summary>
        HTTOP = 12,
        /// <summary>
        /// 可调整大小窗口边框的左上角
        /// </summary>
        HTTOPLEFT = 13,
        /// <summary>
        /// 可调整大小窗口边框的右上角
        /// </summary>
        HTTOPRIGHT = 14,
        /// <summary>
        /// 可调整大小窗口的下边框
        /// </summary>
        HTBOTTOM = 15,
        /// <summary>
        /// 可调整大小窗口边框的左下角
        /// </summary>
        HTBOTTOMLEFT = 16,
        /// <summary>
        /// 可调整大小窗口边框的右下角
        /// </summary>
        HTBOTTOMRIGHT = 17,
        /// <summary>
        /// 不可调整大小窗口的边框
        /// </summary>
        HTBORDER = 18,
        /// <summary>
        /// 最小化按钮（同 HTMINBUTTON）
        /// </summary>
        HTREDUCE = HTMINBUTTON,
        /// <summary>
        /// 最大化按钮（同 HTMAXBUTTON）
        /// </summary>
        HTZOOM = HTMAXBUTTON,
        /// <summary>
        /// 大小调整区域取值范围的起点（同 HTLEFT）
        /// </summary>
        HTSIZEFIRST = HTLEFT,
        /// <summary>
        /// 大小调整区域取值范围的终点（同 HTBOTTOMRIGHT）
        /// </summary>
        HTSIZELAST = HTBOTTOMRIGHT,
        /// <summary>
        /// 对象（保留）
        /// </summary>
        HTOBJECT = 19,
        /// <summary>
        /// 关闭按钮
        /// </summary>
        HTCLOSE = 20,
        /// <summary>
        /// 帮助按钮
        /// </summary>
        HTHELP = 21
    }

    /// <summary>
    /// 表示窗口调整大小时被拖动的边或角。
    /// </summary>
    public enum SizingWindowSide
    {
        /// <summary>
        /// 左边
        /// </summary>
        Left = 1,
        /// <summary>
        /// 右边
        /// </summary>
        Right = 2,
        /// <summary>
        /// 上边
        /// </summary>
        Top = 3,
        /// <summary>
        /// 左上角
        /// </summary>
        TopLeft = 4,
        /// <summary>
        /// 右上角
        /// </summary>
        TopRight = 5,
        /// <summary>
        /// 下边
        /// </summary>
        Bottom = 6,
        /// <summary>
        /// 左下角
        /// </summary>
        BottomLeft = 7,
        /// <summary>
        /// 右下角
        /// </summary>
        BottomRight = 8,
    }

    #endregion

    #region Win32 API 封装方法

    /// <summary>
    /// 将窗口的客户区坐标转换为屏幕坐标。
    /// </summary>
    [SecurityCritical]
    public static void ClientToScreen(HandleRef hWnd, [In, Out] POINT pt)
    {
        if (IntClientToScreen(hWnd, pt) == 0)
            throw new Win32Exception();
    }

    /// <summary>
    /// 获取窗口类信息（适配 32/64 位平台）。
    /// </summary>
    public static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size > 4
            ? GetClassLongPtr64(hWnd, nIndex)
            : new IntPtr(GetClassLongPtr32(hWnd, nIndex));
    }

    /// <summary>
    /// 设置窗口类信息（适配 32/64 位平台）。
    /// </summary>
    public static IntPtr SetClassLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size > 4
            ? SetClassLongPtr64(hWnd, nIndex, dwNewLong)
            : new IntPtr(SetClassLongPtr32(hWnd, nIndex, (uint)dwNewLong.ToInt32()));
    }

    /// <summary>
    /// 获取窗口客户区域矩形。
    /// </summary>
    [SecurityCritical]
    public static void GetClientRect(HandleRef hWnd, [In, Out] ref RECT rect)
    {
        if (!IntGetClientRect(hWnd, ref rect))
            throw new Win32Exception();
    }

    /// <summary>
    /// 将 native 指针转换为指定结构体。
    /// </summary>
    [SecurityCritical]
    public static object PtrToStructure(IntPtr lparam, Type clrType)
    {
        return Marshal.PtrToStructure(lparam, clrType);
    }

    #endregion

    #region 辅助方法
    /// <summary>
    /// 获取 IntPtr 高 16 位（有符号）
    /// </summary>
    public static int SignedHIWORD(IntPtr ptr) => (int)((ptr.ToInt64() >> 16) & 0xFFFF);

    /// <summary>
    /// 获取 IntPtr 低 16 位（有符号）
    /// </summary>
    public static int SignedLOWORD(IntPtr ptr) => (int)(ptr.ToInt64() & 0xFFFF);
    #endregion

    #region Win32 API 导入
    // 所需的Win32 API函数和结构体定义
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    /// <summary>
    /// 设置窗口的位置、大小与 Z 序。失败时返回 false，可通过 Marshal.GetLastWin32Error 取得错误码
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(
       IntPtr hWnd,
       IntPtr hWndInsertAfter,
       int X,
       int Y,
       int cx,
       int cy,
       uint uFlags);

    /// <summary>
    /// 获取窗口所属 DPI 上下文的 DPI 值（Windows 10 1607 及以上）
    /// </summary>
    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(IntPtr hWnd);

    /// <summary>
    /// 获取系统度量值或系统配置（如 SM_CXFRAME），单位通常为像素
    /// </summary>
    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);

    /// <summary>
    /// 获取窗口的屏幕坐标矩形（含非客户区）。失败时返回 false
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    /// <summary>
    /// 获取与指定窗口相交面积最大的显示器句柄（dwFlags 决定无相交时的行为）
    /// </summary>
    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    /// <summary>
    /// 获取显示器信息（整个显示器矩形与工作区矩形）。调用前 MONITORINFO.cbSize 必须已设置
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

    /// <summary>
    /// 向指定窗口同步发送消息，等待窗口过程处理完毕并返回其结果
    /// </summary>
    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// 获取窗口系统菜单（控制菜单）的句柄；bRevert 为 true 时把系统菜单恢复为默认状态
    /// </summary>
    [DllImport("user32.dll")]
    public static extern IntPtr GetSystemMenu(IntPtr hwnd, bool bRevert);

    /// <summary>
    /// 为窗口设置菜单；hMenu 为 IntPtr.Zero 时移除菜单
    /// </summary>
    [DllImport("user32.dll")]
    public static extern bool SetMenu(IntPtr hwnd, IntPtr hMenu);

    /// <summary>
    /// 获取或设置系统级参数（如 SPI_SETDRAGFULLWINDOWS）；fWinIni 决定是否持久化并广播 WM_SETTINGCHANGE
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, string pvParam, uint fWinIni);

    /// <summary>
    /// 加载图标、光标或位图；返回的句柄需按资源类型释放（如 DestroyIcon / DeleteObject）
    /// </summary>
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr LoadImage(IntPtr hinst, IntPtr lpszName, uint uType, int cx, int cy, uint fuLoad);

    [DllImport("user32.dll", EntryPoint = "GetClassLong")]
    private static extern uint GetClassLongPtr32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetClassLongPtr")]
    private static extern IntPtr GetClassLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetClassLong")]
    private static extern uint SetClassLongPtr32(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetClassLongPtr")]
    private static extern IntPtr SetClassLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [SuppressUnmanagedCodeSecurity, SecurityCritical,
     DllImport("user32.dll", EntryPoint = "ClientToScreen", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
    private static extern int IntClientToScreen(HandleRef hWnd, [In, Out] POINT pt);

    [DllImport("user32.dll", EntryPoint = "GetClientRect", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
    private static extern bool IntGetClientRect(HandleRef hWnd, [In, Out] ref RECT rect);

    /// <summary>
    /// 获取包含指定屏幕坐标点的显示器句柄（dwFlags 决定点不在任何显示器上时的行为）
    /// </summary>
    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromPoint(POINT_struct pt, uint dwFlags);

    /// <summary>
    /// 获取鼠标光标的屏幕坐标。失败时返回 false
    /// </summary>
    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT_struct lpPoint);

    /// <summary>
    /// 获取显示器在指定 DPI 类型（如 MDT_EFFECTIVE_DPI）下的 X/Y 方向 DPI。返回 HRESULT，0 表示成功
    /// </summary>
    [DllImport("Shcore.dll")]
    public static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    /// <summary>
    /// 读取窗口的 DWM 属性（DWMWA_*）。返回 HRESULT，0 表示成功
    /// </summary>
    [DllImport("dwmapi.dll")]
    public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

    /// <summary>
    /// 设置窗口的 DWM 属性（DWMWA_*）。返回 HRESULT；当前系统不支持该属性时返回失败码，调用方可忽略
    /// </summary>
    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref uint pvAttribute, int cbAttribute);

    /// <summary>
    /// 创建纯色 GDI 画刷（crColor 为 0x00BBGGRR 的 COLORREF）。返回的句柄必须用 DeleteObject 释放
    /// </summary>
    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateSolidBrush(uint crColor);

    /// <summary>
    /// 删除 GDI 对象（画刷、画笔、位图等）并释放其资源；对象仍被选入设备上下文或被窗口类引用时不能删除
    /// </summary>
    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr hObject);

    /// <summary>
    /// 获取窗口的显示状态与还原/最小化/最大化位置。调用前 WINDOWPLACEMENT.length 必须已设置
    /// </summary>
    [DllImport("user32.dll")]
    public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

    #endregion

    #region 结构体定义

    /// <summary>
    /// 表示窗口最大最小信息结构（用于 WM_GETMINMAXINFO）
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MINMAXINFO
    {
        /// <summary>
        /// 保留，未使用
        /// </summary>
        public POINT ptReserved;
        /// <summary>
        /// 窗口最大化时的宽度（x）与高度（y）
        /// </summary>
        public POINT ptMaxSize;
        /// <summary>
        /// 窗口最大化时左上角的位置
        /// </summary>
        public POINT ptMaxPosition;
        /// <summary>
        /// 拖动调整时允许的最小宽度（x）与高度（y）
        /// </summary>
        public POINT ptMinTrackSize;
        /// <summary>
        /// 拖动调整时允许的最大宽度（x）与高度（y）
        /// </summary>
        public POINT ptMaxTrackSize;
    }

    /// <summary>
    /// Win32 RECT 结构。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        /// <summary>
        /// 矩形左、上、右、下边的坐标（Win32 RECT：右、下边界不包含在矩形内）
        /// </summary>
        public int left, top, right, bottom;

        /// <summary>
        /// 矩形宽度（取绝对值）
        /// </summary>
        public int Width => Math.Abs(right - left);
        /// <summary>
        /// 矩形高度
        /// </summary>
        public int Height => bottom - top;
        /// <summary>
        /// 矩形是否为空（宽度或高度不为正）
        /// </summary>
        public bool IsEmpty => left >= right || top >= bottom;

        /// <summary>
        /// 按四边坐标创建矩形
        /// </summary>
        public RECT(int left, int top, int right, int bottom)
        {
            this.left = left; this.top = top; this.right = right; this.bottom = bottom;
        }

        /// <summary>
        /// 返回矩形四边坐标的文本表示；空矩形返回 "RECT {Empty}"
        /// </summary>
        public override string ToString() =>
            IsEmpty ? "RECT {Empty}" : $"RECT {{ left : {left}, top : {top}, right : {right}, bottom : {bottom} }}";

        /// <summary>
        /// 与另一个 RECT 的四边坐标是否全部相同
        /// </summary>
        public override bool Equals(object obj) => obj is RECT r && this == r;
        /// <summary>
        /// 由四边坐标组合出的哈希值
        /// </summary>
        public override int GetHashCode() => HashCode.Combine(left, top, right, bottom);

        /// <summary>
        /// 两个矩形四边坐标是否全部相同
        /// </summary>
        public static bool operator ==(RECT r1, RECT r2) =>
            r1.left == r2.left && r1.top == r2.top && r1.right == r2.right && r1.bottom == r2.bottom;
        /// <summary>
        /// 两个矩形是否存在不同的边坐标
        /// </summary>
        public static bool operator !=(RECT r1, RECT r2) => !(r1 == r2);
    }

    /// <summary>
    /// 表示 Win32 POINT 结构（值类型）
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT_struct
    {
        /// <summary>
        /// 点的 X 坐标
        /// </summary>
        public int x;
        /// <summary>
        /// 点的 Y 坐标
        /// </summary>
        public int y;

        /// <summary>
        /// 按坐标创建点
        /// </summary>
        public POINT_struct(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    /// <summary>
    /// 表示 Win32 POINT 结构（引用类型）
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class POINT
    {
        /// <summary>
        /// 点的 X 坐标
        /// </summary>
        public int x;
        /// <summary>
        /// 点的 Y 坐标
        /// </summary>
        public int y;
        /// <summary>
        /// 创建坐标为 (0, 0) 的点
        /// </summary>
        public POINT() { }
        /// <summary>
        /// 按坐标创建点
        /// </summary>
        public POINT(int x, int y) { this.x = x; this.y = y; }
    }

    /// <summary>
    /// 表示监视器信息（用于 GetMonitorInfo）
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class MONITORINFO
    {
        /// <summary>
        /// 结构体大小（字节），调用 GetMonitorInfo 前必须设置（默认已初始化）
        /// </summary>
        public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
        /// <summary>
        /// 显示器的完整矩形（屏幕坐标）
        /// </summary>
        public RECT rcMonitor = new RECT();
        /// <summary>
        /// 显示器的工作区矩形（屏幕坐标，不含任务栏等系统保留区域）
        /// </summary>
        public RECT rcWork = new RECT();
        /// <summary>
        /// 显示器标志；MONITORINFOF_PRIMARY（1）表示主显示器
        /// </summary>
        public int dwFlags = 0;
    }

    /// <summary>
    /// 表示窗口位置与状态信息
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        /// <summary>
        /// 结构体大小（字节），调用 GetWindowPlacement 前必须设置
        /// </summary>
        public int length;
        /// <summary>
        /// 位置标志（WPF_*）
        /// </summary>
        public int flags;
        /// <summary>
        /// 显示状态（SW_*，如 SW_SHOWNORMAL / SW_SHOWMINIMIZED / SW_SHOWMAXIMIZED）
        /// </summary>
        public int showCmd;
        /// <summary>
        /// 窗口最小化时左上角的位置
        /// </summary>
        public POINT ptMinPosition;
        /// <summary>
        /// 窗口最大化时左上角的位置
        /// </summary>
        public POINT ptMaxPosition;
        /// <summary>
        /// 窗口处于还原状态时的矩形
        /// </summary>
        public RECT rcNormalPosition;
    }

    #endregion
}
