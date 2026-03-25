// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CaptureScreenshot.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Captures a screen shot using gdi32 functions.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Captures a screen shot using gdi32 functions.
    /// </summary>
    /// <remarks>See https://stackoverflow.com/questions/1736287/capturing-a-window-with-wpf</remarks>
    public static class CaptureScreenshot
    {
        /// <summary>
        /// The ternary raster operations.
        /// </summary>
        private enum TernaryRasterOperations : uint
        {
            /// <summary>
            /// dest = source
            /// </summary>
            SRCCOPY = 0x00CC0020,

            /// <summary>
            /// dest = source OR dest
            /// </summary>
            SRCPAINT = 0x00EE0086,

            /// <summary>
            /// dest = source AND dest
            /// </summary>
            SRCAND = 0x008800C6,

            /// <summary>
            /// dest = source XOR dest
            /// </summary>
            SRCINVERT = 0x00660046,

            /// <summary>
            /// dest = source AND (NOT dest)
            /// </summary>
            SRCERASE = 0x00440328,

            /// <summary>
            /// dest = (NOT source)
            /// </summary>
            NOTSRCCOPY = 0x00330008,

            /// <summary>
            /// dest = (NOT src) AND (NOT dest)
            /// </summary>
            NOTSRCERASE = 0x001100A6,

            /// <summary>
            /// dest = (source AND pattern)
            /// </summary>
            MERGECOPY = 0x00C000CA,

            /// <summary>
            /// dest = (NOT source) OR dest
            /// </summary>
            MERGEPAINT = 0x00BB0226,

            /// <summary>
            /// dest = pattern
            /// </summary>
            PATCOPY = 0x00F00021,

            /// <summary>
            /// dest = DPSnoo
            /// </summary>
            PATPAINT = 0x00FB0A09,

            /// <summary>
            /// dest = pattern XOR dest
            /// </summary>
            PATINVERT = 0x005A0049,

            /// <summary>
            /// dest = (NOT dest)
            /// </summary>
            DSTINVERT = 0x00550009,

            /// <summary>
            /// dest = BLACK
            /// </summary>
            BLACKNESS = 0x00000042,

            /// <summary>
            /// dest = WHITE
            /// </summary>
            WHITENESS = 0x00FF0062
        }

        /// <summary>
        /// 从屏幕捕获指定矩形区域的截图。
        /// 使用 GDI 的 BitBlt 函数实现屏幕像素拷贝。
        /// 所有 GDI 资源在 finally 块中安全释放，防止资源泄漏。
        /// </summary>
        /// <param name="area">要捕获的屏幕矩形区域。</param>
        /// <returns>捕获的位图源。</returns>
        public static BitmapSource Capture(Rect area)
        {
            IntPtr screenDeviceContext = IntPtr.Zero;
            IntPtr memoryDeviceContext = IntPtr.Zero;
            IntPtr bitmapHandle = IntPtr.Zero;

            try
            {
                // 获取整个屏幕的设备上下文
                screenDeviceContext = GetDC(IntPtr.Zero);
                // 创建与屏幕兼容的内存设备上下文
                memoryDeviceContext = CreateCompatibleDC(screenDeviceContext);
                // 创建与屏幕兼容的位图对象
                bitmapHandle = CreateCompatibleBitmap(screenDeviceContext, (int)area.Width, (int)area.Height);
                // 将位图选入内存设备上下文
                SelectObject(memoryDeviceContext, bitmapHandle);

                // 执行屏幕像素到内存位图的拷贝操作
                BitBlt(
                    memoryDeviceContext,
                    0,
                    0,
                    (int)area.Width,
                    (int)area.Height,
                    screenDeviceContext,
                    (int)area.X,
                    (int)area.Y,
                    TernaryRasterOperations.SRCCOPY);

                // 从 GDI 位图句柄创建 WPF 位图源
                var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                    bitmapHandle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                bitmapSource.Freeze();

                return bitmapSource;
            }
            finally
            {
                // 按创建的逆序释放所有 GDI 资源
                if (bitmapHandle != IntPtr.Zero)
                    DeleteObject(bitmapHandle);

                // CreateCompatibleDC 创建的 DC 必须使用 DeleteDC 释放，而非 ReleaseDC
                if (memoryDeviceContext != IntPtr.Zero)
                    DeleteDC(memoryDeviceContext);

                // GetDC 获取的 DC 使用 ReleaseDC 释放
                if (screenDeviceContext != IntPtr.Zero)
                    ReleaseDC(IntPtr.Zero, screenDeviceContext);
            }
        }

        /// <summary>
        /// Gets the cursor position relative to the specified visual.
        /// </summary>
        /// <param name="relativeTo">The visual to relate to.</param>
        /// <returns>
        /// A <see cref="Point" />.
        /// </returns>
        public static Point CorrectGetPosition(Visual relativeTo)
        {
            var w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return relativeTo.PointFromScreen(new Point(w32Mouse.X, w32Mouse.Y));
        }

        /// <summary>
        /// The get mouse screen position.
        /// </summary>
        /// <returns>
        /// The <see cref="Point" />.
        /// </returns>
        public static Point GetMouseScreenPosition()
        {
            var w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }

        /// <summary>
        /// The get cursor pos.
        /// </summary>
        /// <param name="pt">The pt.</param>
        /// <returns>
        /// The get cursor pos.
        /// </returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        /// <summary>
        /// The bit blt.
        /// </summary>
        /// <param name="hdc">The hdc.</param>
        /// <param name="nXDest">The n x dest.</param>
        /// <param name="nYDest">The n y dest.</param>
        /// <param name="nWidth">The n width.</param>
        /// <param name="nHeight">The n height.</param>
        /// <param name="hdcSrc">The hdc src.</param>
        /// <param name="nXSrc">The n x src.</param>
        /// <param name="nYSrc">The n y src.</param>
        /// <param name="dwRop">The dw rop.</param>
        /// <returns>
        /// The bit blt.
        /// </returns>
        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(
            IntPtr hdc,
            int nXDest,
            int nYDest,
            int nWidth,
            int nHeight,
            IntPtr hdcSrc,
            int nXSrc,
            int nYSrc,
            TernaryRasterOperations dwRop);

        /// <summary>
        /// The create bitmap.
        /// </summary>
        /// <param name="nWidth">The n width.</param>
        /// <param name="nHeight">The n height.</param>
        /// <param name="cPlanes">The c planes.</param>
        /// <param name="cBitsPerPel">The c bits per pel.</param>
        /// <param name="lpvBits">The lpv bits.</param>
        /// <returns>
        /// The <see cref="IntPtr" />.
        /// </returns>
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateBitmap(
            int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel, IntPtr lpvBits);

        /// <summary>
        /// The create compatible bitmap.
        /// </summary>
        /// <param name="hdc">The hdc.</param>
        /// <param name="nWidth">The n width.</param>
        /// <param name="nHeight">The n height.</param>
        /// <returns>
        /// The <see cref="IntPtr" />.
        /// </returns>
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        /// <summary>
        /// The create compatible dc.
        /// </summary>
        /// <param name="hdc">The hdc.</param>
        /// <returns>
        /// The <see cref="IntPtr" />.
        /// </returns>
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        /// <summary>
        /// 删除由 CreateCompatibleDC 创建的设备上下文。
        /// </summary>
        /// <param name="hdc">要删除的设备上下文句柄。</param>
        /// <returns>操作是否成功。</returns>
        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        /// <summary>
        /// The delete object.
        /// </summary>
        /// <param name="hObject">The h object.</param>
        /// <returns>
        /// The delete object.
        /// </returns>
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        /// <summary>
        /// The get dc.
        /// </summary>
        /// <param name="hWnd">The h wnd.</param>
        /// <returns>
        /// The <see cref="IntPtr" />.
        /// </returns>
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        /// <summary>
        /// The release dc.
        /// </summary>
        /// <param name="hWnd">The h wnd.</param>
        /// <param name="hDC">The h dc.</param>
        /// <returns>
        /// The release dc.
        /// </returns>
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        /// <summary>
        /// The select object.
        /// </summary>
        /// <param name="hdc">The hdc.</param>
        /// <param name="hgdiobj">The hgdiobj.</param>
        /// <returns>
        /// The <see cref="IntPtr" />.
        /// </returns>
        [DllImport("gdi32.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true)]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        /// <summary>
        /// The win 32 point.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            /// <summary>
            /// The x.
            /// </summary>
            public int X;

            /// <summary>
            /// The y.
            /// </summary>
            public int Y;
        };
    }
}