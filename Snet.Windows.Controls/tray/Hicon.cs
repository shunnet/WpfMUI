// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

// TODO: This class is the only reason for using System.Drawing.Common.
// It is worth looking for a way to get hIcon without using it.
//
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Snet.Windows.Controls.tray;

/// <summary>
/// Facilitates the creation of a hIcon.
/// </summary>
internal static class Hicon
{
    /// <summary>
    /// Tries to take the icon pointer assigned to the application.
    /// </summary>
    /// <summary>
    /// 尝试获取当前应用程序可执行文件关联的图标指针。
    /// 通过 Process.GetCurrentProcess() 获取进程路径，再提取图标句柄。
    /// </summary>
    /// <returns>图标的 HICON 句柄，获取失败返回 IntPtr.Zero。</returns>
    public static IntPtr FromApp()
    {
        try
        {
            // 获取当前进程的主模块文件名
            var processName = Process.GetCurrentProcess().MainModule?.FileName;

            if (string.IsNullOrEmpty(processName))
            {
                return IntPtr.Zero;
            }

            // 从可执行文件中提取关联图标
            var appIconsExtractIcon = System.Drawing.Icon.ExtractAssociatedIcon(processName);

            if (appIconsExtractIcon == null)
            {
                return IntPtr.Zero;
            }

            // 获取图标句柄（注意：图标对象不能在此处 Dispose，否则句柄会失效）
            return appIconsExtractIcon.Handle;
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine(
                $"ERROR | Unable to get application hIcon - {e}",
                "Wpf.Ui.Hicon"
            );
#if DEBUG
            throw;
#else
            return IntPtr.Zero;
#endif
        }
    }

    /// <summary>
    /// 尝试将 WPF ImageSource 转换为 GDI+ Bitmap，并获取其 HICON 指针。
    /// 支持多帧位图（自动选择第一帧）。
    /// 使用 GCHandle 固定像素内存以供 GDI+ 访问。
    /// </summary>
    /// <param name="source">WPF 图像源。</param>
    /// <returns>图标的 HICON 句柄，分配失败返回 IntPtr.Zero。</returns>
    public static IntPtr FromSource(ImageSource source)
    {
        IntPtr hIcon = IntPtr.Zero;
        var bitmapFrame = source as BitmapFrame;

        // 确保源是 BitmapSource 类型
        if (source is not BitmapSource bitmapSource)
        {
            System.Diagnostics.Debug.WriteLine(
                $"ERROR | Unable to allocate hIcon, ImageSource is not a BitmapSource",
                "Wpf.Ui.Hicon"
            );
            return hIcon;
        }

        // 如果是多帧图像，取第一帧
        if ((bitmapFrame?.Decoder?.Frames?.Count ?? 0) > 1)
        {
            bitmapSource = bitmapFrame!.Decoder!.Frames![0];
        }

        // 计算每行像素的字节跨度
        var stride = bitmapSource!.PixelWidth * ((bitmapSource.Format.BitsPerPixel + 7) / 8);
        var pixels = new byte[bitmapSource.PixelHeight * stride];

        // 将像素数据从 BitmapSource 拷贝到字节数组
        bitmapSource.CopyPixels(pixels, stride, 0);

        // 固定像素数组在内存中的位置，防止 GC 移动
        var gcHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);

        if (!gcHandle.IsAllocated)
        {
            System.Diagnostics.Debug.WriteLine(
                $"ERROR | Unable to allocate hIcon, allocation failed.",
                "Wpf.Ui.Hicon"
            );
            return hIcon;
        }

        try
        {
            // 使用 32bpp 预乘 Alpha 格式创建 GDI+ Bitmap
            using var bitmap = new Bitmap(
                bitmapSource.PixelWidth,
                bitmapSource.PixelHeight,
                stride,
                System.Drawing.Imaging.PixelFormat.Format32bppPArgb,
                gcHandle.AddrOfPinnedObject()
            );

            // 从 Bitmap 获取图标句柄
            hIcon = bitmap.GetHicon();
        }
        finally
        {
            // 确保固定句柄被释放，防止内存泄漏
            gcHandle.Free();
        }

        return hIcon;
    }
}
