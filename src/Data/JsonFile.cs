extern alias References;
using System;
using System.IO;
using System.Text;
using References::Newtonsoft.Json;

namespace Oxide.Data
{
    internal class JsonFile : IDataReaderWriter
    {
        public const string FILE_EXTENSION = ".json";

        protected JsonSerializerSettings JsonSettings { get; }

        public JsonFile(JsonSerializerSettings settings = null)
        {
            JsonSettings = settings ?? JsonConvert.DefaultSettings?.Invoke();
        }

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
            using (TextReader txt = new StreamReader(fs, Encoding.UTF8))
            {
                return JsonConvert.DeserializeObject<T>(txt.ReadToEnd(), JsonSettings);
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
            using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
            {
                sw.Write(JsonConvert.SerializeObject(data, JsonSettings));
            }
        }
    }
}
