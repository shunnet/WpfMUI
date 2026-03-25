namespace Snet.Windows.Controls.handler
{
    /// <summary>
    /// Win32 原生对话框封装类<br/>
    /// 基于 COM 的 IFileOpenDialog 接口实现文件/文件夹选择功能<br/>
    /// 支持自定义标题、文件过滤器和文件夹选择模式
    /// </summary>
    public static class Win32Handler
    {
        /// <summary>
        /// IFileOpenDialog COM 接口定义<br/>
        /// 提供 Win32 原生文件打开对话框的功能
        /// </summary>
        [ComImport]
        [Guid("D57C7288-D4AD-4768-BE02-9D969532D960")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig] int Show(IntPtr parent);
            void SetFileTypes(uint cFileTypes, [MarshalAs(UnmanagedType.LPArray)] COMDLG_FILTERSPEC[] rgFilterSpec);
            void SetFileTypeIndex();
            void GetFileTypeIndex();
            void Advise();
            void Unadvise();
            void SetOptions(uint options);
            void GetOptions(out uint options);
            void SetDefaultFolder();
            void SetFolder();
            void GetFolder();
            void GetCurrentSelection();
            void SetFileName();
            void GetFileName();
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);
            void SetOkButtonLabel();
            void SetFileNameLabel();
            void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem item);
            void AddPlace();
            void SetDefaultExtension();
            void Close(int hr);
            void SetClientGuid();
            void ClearClientData();
            void SetFilter();
        }

        /// <summary>
        /// IShellItem COM 接口定义<br/>
        /// 用于获取对话框选中项的文件系统路径
        /// </summary>
        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler();
            void GetParent();
            void GetDisplayName(uint sigdnName, out IntPtr ppszName);
            void GetAttributes();
            void Compare();
        }

        /// <summary>
        /// FileOpenDialog COM 类，用于实例化 IFileOpenDialog
        /// </summary>
        [ComImport]
        [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        private class FileOpenDialog { }

        /// <summary>
        /// 文件类型过滤器结构体<br/>
        /// 对应 Win32 COMDLG_FILTERSPEC，定义过滤器的显示名称和扩展名模式
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct COMDLG_FILTERSPEC
        {
            public string pszName;
            public string pszSpec;

            public COMDLG_FILTERSPEC(string name, string spec)
            {
                pszName = name;
                pszSpec = spec;
            }
        }

        /// <summary>选择文件夹模式标志</summary>
        private const uint FOS_PICKFOLDERS = 0x00000020;
        /// <summary>强制文件系统路径标志</summary>
        private const uint FOS_FORCEFILESYSTEM = 0x00000040;
        /// <summary>文件系统路径显示名称类型</summary>
        private const uint SIGDN_FILESYSPATH = 0x80058000;

        /// <summary>
        /// 打开一个 Win32 原生文件或文件夹选择对话框<br/>
        /// 使用 COM IFileOpenDialog 接口，支持文件过滤器和文件夹选择模式<br/>
        /// 所有 COM 对象均在 finally 块中安全释放，防止内存泄漏
        /// </summary>
        /// <param name="title">对话框标题文本</param>
        /// <param name="selectFolder">是否选择文件夹（true = 选择文件夹；false = 选择文件）</param>
        /// <param name="filters">
        /// 文件类型过滤器（仅当选择文件时生效）<br/>
        /// 格式：键为说明文字，值为文件扩展过滤字符串（如："*.txt;*.docx"）
        /// </param>
        /// <returns>返回选中的完整路径，若用户取消选择则返回空字符串</returns>
        public static string Select(string title, bool selectFolder = false, Dictionary<string, string>? filters = null)
        {
            // 创建 IFileOpenDialog COM 实例
            object? rawDialog = null;
            IShellItem? item = null;
            IntPtr pszString = IntPtr.Zero;

            try
            {
                rawDialog = new FileOpenDialog();
                var dialog = (IFileOpenDialog)rawDialog;

                // 设置对话框选项：必须为文件系统路径
                uint options = FOS_FORCEFILESYSTEM;

                // 如果是选择文件夹，添加 FOS_PICKFOLDERS 选项
                if (selectFolder)
                    options |= FOS_PICKFOLDERS;

                // 应用选项到对话框
                dialog.SetOptions(options);

                // 设置对话框标题
                dialog.SetTitle(title);

                // 设置文件过滤器（仅当不是选择文件夹时）
                if (!selectFolder && filters != null && filters.Count > 0)
                {
                    // 将过滤器字典转换为 COMDLG_FILTERSPEC 数组
                    var specs = filters.Select(f => new COMDLG_FILTERSPEC(f.Key, f.Value)).ToArray();

                    // 应用过滤器到对话框
                    dialog.SetFileTypes((uint)specs.Length, specs);
                }

                // 显示对话框，传入窗口句柄为 IntPtr.Zero（无 owner）
                int hr = dialog.Show(IntPtr.Zero);

                // 如果用户取消或发生错误，则返回空字符串
                if (hr != 0)
                    return string.Empty;

                // 获取用户选择的结果（IShellItem 接口）
                dialog.GetResult(out item);

                // 从 ShellItem 获取实际文件系统路径（SIGDN_FILESYSPATH）
                item.GetDisplayName(SIGDN_FILESYSPATH, out pszString);

                // 将非托管字符串指针转换为 C# 字符串
                string path = Marshal.PtrToStringUni(pszString) ?? string.Empty;

                return path;
            }
            finally
            {
                // 释放非托管字符串内存
                if (pszString != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pszString);

                // 释放 COM 对象，防止内存泄漏
                if (item != null)
                    Marshal.ReleaseComObject(item);

                if (rawDialog != null)
                    Marshal.ReleaseComObject(rawDialog);
            }
        }
    }
}
