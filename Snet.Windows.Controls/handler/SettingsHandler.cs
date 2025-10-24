using Snet.Core.handler;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows;

namespace Snet.Windows.Controls.handler
{
    /// <summary>
    /// 设置处理类
    /// </summary>
    public class SettingsHandler
    {
        /// <summary>
        /// 设置程序是否随系统启动（通过创建/删除启动项 .bat 文件）
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="enable">true 表示开机启动，false 表示关闭</param>
        public void AutoStart(bool enable, string name)
        {
            try
            {
                // 获取当前程序完整路径
                string exeFullPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                                ?? System.Reflection.Assembly.GetEntryAssembly()?.Location
                                ?? AppDomain.CurrentDomain.BaseDirectory;

                // 当前用户的启动文件夹（无需管理员权限）
                string batPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), $"{name}.bat");

                if (enable)
                {
                    string content = $"start \"\" \"{exeFullPath}\"";

                    // 若不存在或内容不同，则重写
                    if (!File.Exists(batPath) ||
                        File.ReadAllText(batPath, Encoding.UTF8).Trim() != content.Trim())
                    {
                        File.WriteAllText(batPath, content, Encoding.UTF8);
                    }
                }
                else
                {
                    // 删除启动文件
                    if (File.Exists(batPath))
                        File.Delete(batPath);
                }
            }
            catch (Exception ex)
            {
                // 捕获异常写入调试输出，避免启动时弹窗
                throw new Exception($"[AutoStart] {ex}", ex);
            }
        }

        /// <summary>
        /// 检查是否已设置为开机启动
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns>true 表示存在启动项</returns>
        public bool IsAutoStartEnabled(string name)
        {
            string batPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), $"{name}.bat");
            return File.Exists(batPath);
        }

        /// <summary>
        /// 检查当前进程是否已以管理员身份运行
        /// </summary>
        public bool IsRunAsAdmin()
        {
            try
            {
                using WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 如果不是管理员，则以管理员身份重新启动当前程序
        /// </summary>
        /// <param name="arguments">启动参数，可选</param>
        /// <param name="showMessage">是否提示用户</param>
        public void RestartAsAdmin(string? arguments = null, bool showMessage = true)
        {
            try
            {
                if (IsRunAsAdmin())
                    return; // 已经是管理员，无需重启

                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? AppContext.BaseDirectory + AppDomain.CurrentDomain.FriendlyName;

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments ?? string.Empty,
                    UseShellExecute = true,
                    Verb = "runas", // 关键点：触发 UAC 提权
                    WorkingDirectory = AppContext.BaseDirectory
                };

                Process.Start(psi);
                Application.Current.Shutdown(); // 退出当前非管理员实例
            }
            catch (Exception ex)
            {
                if (showMessage)
                    _ = Snet.Windows.Controls.message.MessageBox.Show(ex.Message, LanguageHandler.GetLanguageValue("提示", new("Snet.Windows.Controls", "Language", "Snet.Windows.Controls.dll")), Snet.Windows.Controls.@enum.MessageBoxButton.OK, Snet.Windows.Controls.@enum.MessageBoxImage.Exclamation).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 创建桌面快捷方式
        /// </summary>
        /// <param name="shortcutName">快捷方式名称（不含 .lnk）</param>
        /// <param name="targetPath">目标程序路径</param>
        /// <param name="description">描述文本，可选</param>
        /// <param name="iconPath">图标路径，可选（为空则使用目标程序图标）</param>
        /// <param name="arguments">启动参数，可选</param>
        /// <param name="workingDirectory">工作目录，可选（默认为程序所在目录）</param>
        public void CreateDesktopShortcut(string shortcutName, string targetPath, string description = "", string iconPath = "", string arguments = "", string workingDirectory = "")
        {
            if (string.IsNullOrWhiteSpace(targetPath) || !File.Exists(targetPath))
                throw new FileNotFoundException("目标程序不存在", targetPath);

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string shortcutPath = Path.Combine(desktop, $"{shortcutName}.lnk");

            // 若未指定工作目录，则使用目标程序所在目录
            if (string.IsNullOrWhiteSpace(workingDirectory))
                workingDirectory = Path.GetDirectoryName(targetPath)!;

            IShellLinkW link = (IShellLinkW)new ShellLink();
            link.SetPath(targetPath);
            link.SetArguments(arguments);
            link.SetDescription(description);
            link.SetWorkingDirectory(workingDirectory);
            link.SetIconLocation(string.IsNullOrWhiteSpace(iconPath) ? targetPath : iconPath, 0);

            // 通过 IPersistFile 保存为 .lnk 文件
            IPersistFile file = (IPersistFile)link;
            file.Save(shortcutPath, false);
        }

        /// <summary>
        /// 检查桌面上是否存在指定名称的快捷方式（.lnk 文件）
        /// </summary>
        /// <param name="shortcutName">快捷方式名称（不含扩展名）</param>
        /// <param name="forAllUsers">是否检查“所有用户桌面”，否则为当前用户桌面</param>
        /// <returns>存在返回 true，否则 false</returns>
        public bool DesktopShortcutExists(string shortcutName, bool forAllUsers = false)
        {
            try
            {
                string desktopPath = forAllUsers
                    ? Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
                    : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

                string shortcutPath = Path.Combine(desktopPath, $"{shortcutName}.lnk");

                return File.Exists(shortcutPath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 删除桌面快捷方式
        /// </summary>
        /// <param name="shortcutName">快捷方式名称（不含 .lnk）</param>
        public void DeleteDesktopShortcut(string shortcutName)
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string shortcutPath = Path.Combine(desktop, $"{shortcutName}.lnk");
            if (File.Exists(shortcutPath))
                File.Delete(shortcutPath);
        }

        #region ShellLink COM 接口定义
        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        internal class ShellLink { }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        internal interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [Guid("0000010B-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IPersistFile
        {
            void GetClassID(out Guid pClassID);
            void IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
        }
        #endregion
    }
}
