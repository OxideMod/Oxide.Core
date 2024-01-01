extern alias References;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Oxide.Data.Formatters;
using References::Newtonsoft.Json;
using References::Newtonsoft.Json.Serialization;
using Formatting = References::Newtonsoft.Json.Formatting;

namespace Oxide.Data.StorageDrivers
{
    internal class FileDriver : IFormattableStorageDriver
    {
        public Uri BaseDirectory { get; }

        public IDataFormatter DefaultFormatter { get; private set; }

        private readonly ICollection<IDataFormatter> formatters;

        public IEnumerable<IDataFormatter> Formatters => formatters.ToArray();

        public FileDriver(string baseDirectory, ICollection<IDataFormatter> formatters, IDataFormatter defaultFormatter = null)
        {
            if (!baseDirectory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                baseDirectory += Path.DirectorySeparatorChar;
            }

            BaseDirectory = new Uri(baseDirectory, UriKind.Absolute);
            this.formatters = formatters ?? new List<IDataFormatter>();
            DefaultFormatter = defaultFormatter ?? formatters.First();
        }

        #region Reading / Writing

        public void Write(string key, object data) => Write(key, data, DefaultFormatter);

        public object Read(string key, Type context, object existingGraph = null) => Read(key, context, DefaultFormatter, existingGraph);

        public void Write(string key, object data, IDataFormatter formatter)
        {
            using (FileStream fs = GetFileStream(key, true, formatter))
            {
                formatter.Serialize(fs, data.GetType(), data);
            }
        }

        public object Read(string key, Type context, IDataFormatter formatter, object existingGraph = null)
        {
            using (FileStream fs = GetFileStream(key, false, formatter))
            {
                return formatter.Deserialize(context, fs, existingGraph);
            }
        }

        #endregion

        #region File Operations

        protected virtual IDataFormatter GetFormatter(string fileExtension, bool throwOnMissing = true)
        {
            lock (formatters)
            {
                foreach (IDataFormatter formatter in formatters)
                {
                    if (formatter.FileExtension.Equals(fileExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        return formatter;
                    }
                }
            }

            if (throwOnMissing)
            {
                throw new InvalidOperationException($"No formatter found for File Extension '{fileExtension}'");
            }

            return null;
        }

        protected virtual FileStream GetFileStream(string key, bool needWrite, IDataFormatter formatter)
        {
            Uri target = Path.IsPathRooted(key) ? new Uri(key, UriKind.Absolute) : new Uri(BaseDirectory, key);

            if (!target.AbsolutePath.StartsWith(BaseDirectory.AbsolutePath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Path must be relative or absolute path must contain '{BaseDirectory.AbsolutePath}'");
            }

            string directory = Path.GetDirectoryName(target.LocalPath);
            string fileName = Path.GetFileNameWithoutExtension(target.LocalPath);
            string ext = Path.GetExtension(target.LocalPath);

            if (string.IsNullOrEmpty(ext))
            {
                ext = formatter.FileExtension;
            }

            if (!Directory.Exists(directory))
            {
                if (!needWrite)
                {
                    return null;
                }

                Directory.CreateDirectory(directory);
            }

            string fullPath = Path.Combine(directory, fileName + ext);

            if (needWrite)
            {
                return File.Exists(fullPath)
                    ? File.Open(fullPath, FileMode.Truncate, FileAccess.Write, FileShare.Read)
                    : File.Open(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            }

            return File.Exists(fullPath)
                ? File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read)
                : null;
        }

        #endregion

        #region Formatters

        public void UpdateFormatters(Func<ICollection<IDataFormatter>, IDataFormatter> formatCallback)
        {
            if (formatCallback == null) return;

            lock (formatters)
            {
                DefaultFormatter = formatCallback(formatters);
                Debug.WriteLine($"Default formatter updated to {DefaultFormatter?.MimeType ?? "null"}");
            }
        }

        #endregion

        #region Static

        public static IFormattableStorageDriver CreateDefault(string baseDirectory) => new FileDriver(baseDirectory,
            new List<IDataFormatter>()
            {
                new JsonFormatter(),
                new YamlFormatter(),
                new ProtobufFormatter(),
                new XmlFormatter()
            });

        #endregion
    }
}
