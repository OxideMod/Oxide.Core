using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Oxide.Core;

namespace Oxide.IO.Windows
{
    internal class WindowsFileSystem : IFileSystem
    {
        private const int MAX_BUFFERS = 5;

        #region API

        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        private const uint FILE_READ_EA = 0x0008;
        private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x2000000;

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint GetFinalPathNameByHandle(
            IntPtr hFile,
            [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszFilePath,
            uint cchFilePath,
            uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateFile(
            [MarshalAs(UnmanagedType.LPTStr)] string filename,
            [MarshalAs(UnmanagedType.U4)] uint access,
            [MarshalAs(UnmanagedType.U4)] FileShare share,
            IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] uint flagsAndAttributes,
            IntPtr templateFile);

        #endregion

        private Queue<StringBuilder> StrBuffers { get; } = new Queue<StringBuilder>();

        public bool IsSymbolicLink(string path)
        {
            if (File.Exists(path) || Directory.Exists(path))
            {
                return (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
            }

            return false;
        }

        public string ResolvePath(string path)
        {
            if (!IsSymbolicLink(path))
            {
                path = Path.GetFullPath(path);
                return path;
            }

            IntPtr handle = CreateFile(path, FILE_READ_EA, FileShare.ReadWrite | FileShare.Delete,
                                       IntPtr.Zero, FileMode.Open, FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);

            if (handle == INVALID_HANDLE_VALUE)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            StringBuilder str;

            lock (StrBuffers)
            {
                str = StrBuffers.Count == 0 ? new StringBuilder(1024) : StrBuffers.Dequeue();
            }

            str.Length = 0;
            try
            {
                uint fl = GetFinalPathNameByHandle(handle, str, 1024, 0);

                if (fl == 0)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                path = str.ToString().TrimStart('\\', '?');
                Interface.Oxide.LogDebug($"Symbolic: {path}");
                return path;
            }
            finally
            {
                CloseHandle(handle);

                lock (StrBuffers)
                {
                    if (StrBuffers.Count < MAX_BUFFERS)
                    {
                        str.Length = 0;
                        StrBuffers.Enqueue(str);
                    }
                }
            }
        }
    }
}
