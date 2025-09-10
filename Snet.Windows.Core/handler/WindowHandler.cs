using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace Snet.Windows.Core.handler;

/// <summary>
/// �ṩ��ԭ�� Win32 ���ڲ����ķ�װ�������ڣ�
/// - WPF �ޱ߿򴰿ڵ����/��С������
/// - �Զ����������ק��������С
/// - DPI ������ϵͳ�߿�����
/// </summary>
public static class WindowHandler
{
    #region ��������
    /// <summary>
    /// �����ļ�����·��
    /// </summary>
    public static string BasePath = Path.Combine(AppContext.BaseDirectory, "config");
    #endregion

    #region �������壨Win32 ��Ϣ��ϵͳָ�꣩
    public const int SM_CXFRAME = 32;               // ���ڱ߿��ȣ�X��
    public const int SM_CYFRAME = 33;               // ���ڱ߿��ȣ�Y��
    public const int SM_CXPADDEDBORDER = 92;        // ���ڱ߿�Ķ������

    public const uint SWP_NOZORDER = 0x0004;        // SetWindowPos: ���ı䴰�ڵ� Z ��
    public const uint SWP_NOACTIVATE = 0x0010;      // SetWindowPos: �������

    public const int MONITOR_DEFAULTTONEAREST = 0x0002; // ��ȡ��ӽ����ڵ���ʾ��

    public const int WM_GETMINMAXINFO = 0x0024;     // ��ȡ�������/��С��С��Ϣ
    public const int WM_MOVE = 0x0003;              // �����ƶ���Ϣ
    public const int WM_NCHITTEST = 0x0084;         // ������в���
    public const int WM_SIZING = 0x0214;            // ���ڵ�����С��Ϣ

    public const int MDT_EFFECTIVE_DPI = 0;         // ʵ�� DPI���������ţ�

    // DWM ����
    public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
    public const int DWMWA_VISIBLE_FRAME_BORDER_THICKNESS = 37;
    #endregion

    #region ö�ٶ���

    /// <summary>
    /// ������в��Խ��������������ڴ�������
    /// </summary>
    public enum HitTestResult
    {
        HTERROR = -2,
        HTTRANSPARENT = -1,
        HTNOWHERE = 0,
        HTCLIENT = 1,
        HTCAPTION = 2,
        HTSYSMENU = 3,
        HTGROWBOX = 4,
        HTSIZE = HTGROWBOX,
        HTMENU = 5,
        HTHSCROLL = 6,
        HTVSCROLL = 7,
        HTMINBUTTON = 8,
        HTMAXBUTTON = 9,
        HTLEFT = 10,
        HTRIGHT = 11,
        HTTOP = 12,
        HTTOPLEFT = 13,
        HTTOPRIGHT = 14,
        HTBOTTOM = 15,
        HTBOTTOMLEFT = 16,
        HTBOTTOMRIGHT = 17,
        HTBORDER = 18,
        HTREDUCE = HTMINBUTTON,
        HTZOOM = HTMAXBUTTON,
        HTSIZEFIRST = HTLEFT,
        HTSIZELAST = HTBOTTOMRIGHT,
        HTOBJECT = 19,
        HTCLOSE = 20,
        HTHELP = 21
    }

    /// <summary>
    /// ��ʾ���ڵ�����Сʱ���϶��ı߻�ǡ�
    /// </summary>
    public enum SizingWindowSide
    {
        Left = 1,
        Right = 2,
        Top = 3,
        TopLeft = 4,
        TopRight = 5,
        Bottom = 6,
        BottomLeft = 7,
        BottomRight = 8,
    }

    #endregion

    #region Win32 API ��װ����

    /// <summary>
    /// �����ڵĿͻ�������ת��Ϊ��Ļ���ꡣ
    /// </summary>
    [SecurityCritical]
    public static void ClientToScreen(HandleRef hWnd, [In, Out] POINT pt)
    {
        if (IntClientToScreen(hWnd, pt) == 0)
            throw new Win32Exception();
    }

    /// <summary>
    /// ��ȡ��������Ϣ������ 32/64 λƽ̨����
    /// </summary>
    public static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size > 4
            ? GetClassLongPtr64(hWnd, nIndex)
            : new IntPtr(GetClassLongPtr32(hWnd, nIndex));
    }

    /// <summary>
    /// ��ȡ���ڿͻ�������Ρ�
    /// </summary>
    [SecurityCritical, SecurityTreatAsSafe]
    public static void GetClientRect(HandleRef hWnd, [In, Out] ref RECT rect)
    {
        if (!IntGetClientRect(hWnd, ref rect))
            throw new Win32Exception();
    }

    /// <summary>
    /// �� native ָ��ת��Ϊָ���ṹ�塣
    /// </summary>
    [SecurityCritical]
    public static object PtrToStructure(IntPtr lparam, Type clrType)
    {
        return Marshal.PtrToStructure(lparam, clrType);
    }

    #endregion

    #region ��������
    /// <summary>
    /// ��ȡ IntPtr �� 16 λ���з��ţ�
    /// </summary>
    public static int SignedHIWORD(IntPtr ptr) => (int)((ptr.ToInt64() >> 16) & 0xFFFF);

    /// <summary>
    /// ��ȡ IntPtr �� 16 λ���з��ţ�
    /// </summary>
    public static int SignedLOWORD(IntPtr ptr) => (int)(ptr.ToInt64() & 0xFFFF);
    #endregion

    #region Win32 API ����
    // �����Win32 API�����ͽṹ�嶨��
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(
       IntPtr hWnd,
       IntPtr hWndInsertAfter,
       int X,
       int Y,
       int cx,
       int cy,
       uint uFlags);

    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern IntPtr GetSystemMenu(IntPtr hwnd, bool bRevert);

    [DllImport("user32.dll")]
    public static extern bool SetMenu(IntPtr hwnd, IntPtr hMenu);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, string pvParam, uint fWinIni);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr LoadImage(IntPtr hinst, IntPtr lpszName, uint uType, int cx, int cy, uint fuLoad);

    [DllImport("user32.dll", EntryPoint = "GetClassLong")]
    private static extern uint GetClassLongPtr32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetClassLongPtr")]
    private static extern IntPtr GetClassLongPtr64(IntPtr hWnd, int nIndex);

    [SuppressUnmanagedCodeSecurity, SecurityCritical,
     DllImport("user32.dll", EntryPoint = "ClientToScreen", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
    private static extern int IntClientToScreen(HandleRef hWnd, [In, Out] POINT pt);

    [DllImport("user32.dll", EntryPoint = "GetClientRect", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
    private static extern bool IntGetClientRect(HandleRef hWnd, [In, Out] ref RECT rect);

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromPoint(POINT_struct pt, uint dwFlags);

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT_struct lpPoint);

    [DllImport("Shcore.dll")]
    public static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("dwmapi.dll")]
    public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

    [DllImport("user32.dll")]
    public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

    #endregion

    #region �ṹ�嶨��

    /// <summary>
    /// ��ʾ���������С��Ϣ�ṹ������ WM_GETMINMAXINFO��
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    /// <summary>
    /// Win32 RECT �ṹ��
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left, top, right, bottom;

        public int Width => Math.Abs(right - left);
        public int Height => bottom - top;
        public bool IsEmpty => left >= right || top >= bottom;

        public RECT(int left, int top, int right, int bottom)
        {
            this.left = left; this.top = top; this.right = right; this.bottom = bottom;
        }

        public override string ToString() =>
            IsEmpty ? "RECT {Empty}" : $"RECT {{ left : {left}, top : {top}, right : {right}, bottom : {bottom} }}";

        public override bool Equals(object obj) => obj is RECT r && this == r;
        public override int GetHashCode() => left ^ top ^ right ^ bottom;

        public static bool operator ==(RECT r1, RECT r2) =>
            r1.left == r2.left && r1.top == r2.top && r1.right == r2.right && r1.bottom == r2.bottom;
        public static bool operator !=(RECT r1, RECT r2) => !(r1 == r2);
    }

    /// <summary>
    /// ��ʾ Win32 POINT �ṹ��ֵ���ͣ�
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT_struct
    {
        public int x;
        public int y;

        public POINT_struct(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    /// <summary>
    /// ��ʾ Win32 POINT �ṹ���������ͣ�
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class POINT
    {
        public int x;
        public int y;
        public POINT() { }
        public POINT(int x, int y) { this.x = x; this.y = y; }
    }

    /// <summary>
    /// ��ʾ��������Ϣ������ GetMonitorInfo��
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class MONITORINFO
    {
        public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
        public RECT rcMonitor = new RECT();
        public RECT rcWork = new RECT();
        public int dwFlags = 0;
    }

    /// <summary>
    /// ��ʾ����λ����״̬��Ϣ
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        public int length;
        public int flags;
        public int showCmd;
        public POINT ptMinPosition;
        public POINT ptMaxPosition;
        public RECT rcNormalPosition;
    }

    #endregion
}
