extern alias References;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Oxide.Core;
using References::Mono.Unix.Native;

namespace Oxide.IO.Unix
{
    internal class UnixFileSystemWatcher : CachedFileSystemWatcher
    {
        #region Structures

        [StructLayout(LayoutKind.Sequential)]
        private struct InotifyEvent
        {
            public int Descriptor;
            public NotifyMask Mask;
            public uint Cookie;
            public uint Length;
        }

        #endregion

        private readonly int fileDescriptor;
        private readonly Thread worker;
        private volatile bool is_working;

        [DllImport("libc", SetLastError = true)]
        private static extern int inotify_init();

        [DllImport("libc", SetLastError = true)]
        private static extern int inotify_add_watch(int fd, string pathname, uint mask);

        [DllImport("libc", SetLastError = true)]
        private static extern int inotify_rm_watch(int fd, int wd);

        [DllImport("libc", SetLastError = true)]
        private static extern int read(int fd, byte[] buffer, int count);

        [DllImport("libc", SetLastError = true)]
        private static extern int close(int fd);

        #region Initialization

        public UnixFileSystemWatcher(IFileSystem fs, string directory, bool subDirs, NotifyMask filter, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase) : base(directory, subDirs, filter, fs, stringComparison)
        {
            fileDescriptor = inotify_init();
            worker = new Thread(DoWork)
            {
                Name = "Oxide_" + nameof(UnixFileSystemWatcher),
                IsBackground = true,
                CurrentCulture = Thread.CurrentThread.CurrentCulture,
                CurrentUICulture = Thread.CurrentThread.CurrentUICulture
            };

            if (fileDescriptor != -1)
            {
                is_working = true;
                return;
            }

            Errno err = Stdlib.GetLastError();
            throw new IOException($"Failed to initialize inotify: {err}", (int)err);
        }

        ~UnixFileSystemWatcher()
        {
            ReleaseUnmanagedResources();
        }

        public override void EndInit()
        {
            base.EndInit();
            worker.Start();
        }

        #endregion

        #region FileSystem Dependent

        protected override int SubscribeTo(string directory)
        {
            int result = inotify_add_watch(fileDescriptor, directory, (uint)Filter);

            if (result != -1)
            {
                LogDebug($"Subscribed to inotify handle {result} | {directory}");
                return result;
            }

            Errno err = Stdlib.GetLastError();

            try
            {
                throw new IOException($"Failed to subscribe to '{directory}'", result);
            }
            catch (Exception e)
            {
                OnFileSystemError(e);
                throw;
            }
        }

        protected override bool UnsubscribeFrom(int id)
        {
            int result = inotify_rm_watch(fileDescriptor, id);

            if (result != -1)
            {
                LogDebug($"Unsubscribed from inotify handle '{id}'");

            }

            return true;
        }

        #endregion

        private void DoWork()
        {
            byte[] buffer = new byte[4096];

            while (is_working)
            {
                int length = read(fileDescriptor, buffer, buffer.Length);

                if (length > 0)
                {
                    for (int i = 0; i < length;)
                    {
                        uint size = 0;
                        string name = null;
                        string parent = null;
                        InotifyEvent evt = default;
                        try
                        {
                            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                            IntPtr n = new IntPtr(handle.AddrOfPinnedObject().ToInt64() + i);
                            evt = (InotifyEvent)Marshal.PtrToStructure(n, typeof(InotifyEvent));
                            size = evt.Length;

                            if (evt.Length > 0)
                            {
                                name = Encoding.UTF8
                                               .GetString(buffer, i + Marshal.SizeOf(typeof(InotifyEvent)), (int)size)
                                               .TrimEnd('\0');
                            }
                            handle.Free();

                            parent = GetDirectoryById(evt.Descriptor);
                            OnFileSystemEvent(parent, name, evt.Mask);
                        }
                        catch (Exception e)
                        {
                            Interface.Oxide.LogException($"Failed to read change | Parent: {parent}, Name: {name}, {evt.Mask}", e);
                            OnFileSystemError(e);
                        }
                        finally
                        {
                            i += Marshal.SizeOf(typeof(InotifyEvent)) + (int)size;
                        }
                    }
                }
            }
        }

        protected override void OnFileSystemEvent(string directory, string name, NotifyMask mask)
        {
            directory = FileSystem.ResolvePath(directory);
            string fullPath = string.IsNullOrEmpty(name) ? directory : Path.Combine(directory, name);
            CleanName(fullPath, out directory, out name);
            base.OnFileSystemEvent(directory, name, mask);
        }

        private void ReleaseUnmanagedResources()
        {
            is_working = false;
            worker.Join();
            close(fileDescriptor);
        }

        protected override void OnDispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
    }
}
