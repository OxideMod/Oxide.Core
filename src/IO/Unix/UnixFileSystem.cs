extern alias References;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using References::Mono.Unix.Native;

namespace Oxide.IO.Unix
{
    internal class UnixFileSystem : IFileSystem
    {
        private const int MAX_BUFFERS = 5;
        private const int MAX_PATH_BUFFER = 4096;
        private Queue<StringBuilder> StrBuffers { get; } = new Queue<StringBuilder>();

        public bool IsSymbolicLink(string path)
        {
            int result = Syscall.lstat(path, out Stat buffer);

            if (result == -1)
            {
                Errno err = Stdlib.GetLastError();

                switch (err)
                {
                    default:
                        return false;

                    case Errno.EACCES:
                        throw new AccessViolationException("Search permission is denied for a component of the path prefix.");
                }
            }

            return (buffer.st_mode & FilePermissions.S_IFMT) == FilePermissions.S_IFMT;
        }

        public string ResolvePath(string path)
        {
            if (!IsSymbolicLink(path))
            {
                return Path.GetFullPath(path);
            }

            StringBuilder str = null;

            lock (StrBuffers)
            {
                str = StrBuffers.Count == 0 ? new StringBuilder(MAX_PATH_BUFFER) : StrBuffers.Dequeue();
            }

            try
            {
                int result = Syscall.readlink(path, str, MAX_PATH_BUFFER);

                if (result != -1)
                {
                    return str.ToString();
                }

                Errno err = Stdlib.GetLastError();
                throw new IOException($"Unix filesystem returned: {err}");
            }
            finally
            {
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
