extern alias References;

using References::ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;

namespace Umod
{
    public class ProtoStorage
    {
        public static IEnumerable<string> GetFiles(string subDirectory)
        {
            string directory = GetFileDataPath(subDirectory.Replace("..", ""));
            if (!Directory.Exists(directory))
            {
                yield break;
            }

            foreach (string file in Directory.GetFiles(directory, "*.data"))
            {
                yield return Utility.GetFileNameWithoutExtension(file);
            }
        }

        public static T Load<T>(params string[] subPaths)
        {
            string name = GetFileName(subPaths);
            string path = GetFileDataPath(name);
            try
            {
                if (File.Exists(path))
                {
                    T data;
                    using (FileStream file = File.OpenRead(path))
                    {
                        data = Serializer.Deserialize<T>(file);
                    }

                    return data;
                }
            }
            catch (Exception ex)
            {
                Interface.Umod.LogException($"Failed to load protobuf data from {name}", ex);
            }
            return default(T);
        }

        public static void Save<T>(T data, params string[] subPaths)
        {
            string name = GetFileName(subPaths);
            string path = GetFileDataPath(name);
            string directory = Path.GetDirectoryName(path);
            try
            {
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (FileStream file = File.Open(path, FileMode.Create))
                {
                    Serializer.Serialize(file, data);
                }
            }
            catch (Exception ex)
            {
                Interface.Umod.LogException($"Failed to save protobuf data to {name}", ex);
            }
        }

        public static bool Exists(params string[] subPaths) => File.Exists(GetFileDataPath(GetFileName(subPaths)));

        public static string GetFileName(params string[] subPaths)
        {
            return string.Join(Path.DirectorySeparatorChar.ToString(), subPaths).Replace("..", "") + ".data";
        }

        public static string GetFileDataPath(string name) => Path.Combine(Interface.Umod.DataDirectory, name);
    }
}
