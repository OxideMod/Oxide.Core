using System;
using System.IO;

namespace Oxide.Data
{
    internal struct DataFileSettings
    {
        private static char[] split = new char[] { ';' };

        public string Directory { get; }

        public string Name { get; }

        public string Extension { get; }

        public DataFileSettings(string str)
        {
            string[] fs = str.Split(split, StringSplitOptions.RemoveEmptyEntries);

            if (fs.Length == 0)
            {
                Directory = Path.GetDirectoryName(str);
                Name = Path.GetFileNameWithoutExtension(str);
                Extension = Path.GetExtension(str);
            }
            else
            {
                Directory = null;
                Name = null;
                Extension = null;
                for (int i = 0; i < fs.Length; i++)
                {
                    string c = fs[i];
                    int index = c.IndexOf('=', 0);
                    if (c.StartsWith("fn=", StringComparison.OrdinalIgnoreCase) ||
                        c.StartsWith("filename=", StringComparison.OrdinalIgnoreCase))
                    {
                        Name = c.Substring(index);
                    }
                    else if (c.StartsWith("fe=", StringComparison.OrdinalIgnoreCase) ||
                             c.StartsWith("extension=", StringComparison.OrdinalIgnoreCase))
                    {
                        Extension = c.Substring(index);
                    }
                    else if (c.StartsWith("dir=", StringComparison.OrdinalIgnoreCase) ||
                             c.StartsWith("directory=", StringComparison.OrdinalIgnoreCase))
                    {
                        Directory = c.Substring(index);
                    }
                }
            }

            if (string.IsNullOrEmpty(Name))
            {
                throw new ArgumentNullException(nameof(Name));
            }

            if (string.IsNullOrEmpty(Directory))
            {
                string dir = Path.GetDirectoryName(Name);

                if (!string.IsNullOrEmpty(dir))
                {
                    Directory = dir;
                    Name = Path.GetFileName(Name);
                }
            }

            if (!string.IsNullOrEmpty(Extension)) return;

            string ext = Path.GetExtension(Name);

            if (string.IsNullOrEmpty(ext)) return;
            Extension = ext;
            Name = Path.GetFileNameWithoutExtension(Name);
        }
    }
}
