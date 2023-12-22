extern alias References;
using System;
using System.IO;
using References::ProtoBuf;

namespace Oxide.Data
{
    internal class ProtobufFile : IDataReaderWriter
    {
        public const string FILE_EXTENSION = ".data";

        public T Read<T>(string sourceStr)
        {
            if (string.IsNullOrEmpty(sourceStr))
            {
                throw new ArgumentNullException(nameof(sourceStr));
            }

            string ext = Path.GetExtension(sourceStr);

            if (string.IsNullOrEmpty(ext))
            {
                sourceStr = Path.ChangeExtension(sourceStr, FILE_EXTENSION);
                ext = FILE_EXTENSION;
            }
            else if (!String.Equals(ext, FILE_EXTENSION, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidCastException($"Attempted to read a {GetType().Name} but extension is invalid (Needs: {FILE_EXTENSION} | Has: {ext})");
            }

            using (FileStream fs = File.OpenRead(sourceStr))
            {
                return Serializer.Deserialize<T>(fs);
            }
        }

        public void Write<T>(T data, string destinationStr)
        {
            if (string.IsNullOrEmpty(destinationStr))
            {
                throw new ArgumentNullException(nameof(destinationStr));
            }

            string ext = Path.GetExtension(destinationStr);

            if (string.IsNullOrEmpty(ext))
            {
                destinationStr = Path.ChangeExtension(destinationStr, FILE_EXTENSION);
                ext = FILE_EXTENSION;
            }
            else if (!String.Equals(ext, FILE_EXTENSION, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidCastException($"Attempted to write a {GetType().Name} but extension is invalid (Needs: {FILE_EXTENSION} | Has: {ext})");
            }

            using (FileStream fs = File.Exists(destinationStr)
                       ? File.Open(destinationStr, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite)
                       : File.Open(destinationStr, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
            {
                Serializer.Serialize(fs, data);
            }
        }
    }
}
