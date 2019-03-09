using System;
using System.IO;
using System.Text;

namespace uMod.ObjectStream.Data
{
    [Serializable]
    class CompilerFile
    {
        public string Name { get; set; }
        public string Directory { get; set; }
        public byte[] Data { get; set; }
        public string Encoding { get; set; }

        public CompilerFile(string name, byte[] data, string directory, Encoding encoding)
        {
            Name = name;
            Directory = directory;
            Data = data;
            Encoding = encoding != null ? encoding.EncodingName : System.Text.Encoding.UTF8.EncodingName;
        }

        public CompilerFile(string directory, string name, Encoding encoding = null)
        {
            Name = name;
            Directory = directory;
            string path = Path.Combine(directory, Name);
            Data = File.ReadAllBytes(path);
            Encoding = encoding != null ? encoding.EncodingName : GetFileEncoding(path, System.Text.Encoding.UTF8).EncodingName;
        }

        public CompilerFile(string path, Encoding encoding = null)
        {
            Name = Path.GetFileName(path);
            Directory = Path.GetDirectoryName(path);
            Data = File.ReadAllBytes(path);
            Encoding = encoding != null ? encoding.EncodingName : GetFileEncoding(path, System.Text.Encoding.UTF8).EncodingName;
        }

        private static Encoding GetFileEncoding(string fileName, Encoding defaultEncodingIfNoBom)
        {
            try
            {
                if (!File.Exists(fileName))
                {
                    return defaultEncodingIfNoBom;
                }

                using (FileStream stream = new FileStream(fileName, FileMode.Open))
                using (StreamReader reader = new StreamReader(stream, defaultEncodingIfNoBom, true))
                {
                    reader.Peek();
                    return reader.CurrentEncoding;
                }
            }
            catch (Exception) { }

            return defaultEncodingIfNoBom;
        }
    }
}
