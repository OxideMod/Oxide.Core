using System;
using System.ComponentModel;
using System.IO;

namespace Oxide.Data
{
    public static class DataHelpers
    {
        public static string GetFileNameFromType<T>(string extension = null) =>
            GetFileNameFromType(typeof(T), extension);

        public static string GetFileNameFromType(Type type, string extension = null)
        {
            string name = type.Name;

            object[] attributes = type.GetCustomAttributes(typeof(DisplayNameAttribute), false);

            if (attributes.Length != 0)
            {
                DisplayNameAttribute title = (DisplayNameAttribute)attributes[0];
                name = title.DisplayName;
            }


            name = CleanFileName(name);

            if (!string.IsNullOrEmpty(extension))
            {
                name = Path.ChangeExtension(name, extension);
            }

            return name;
        }

        public static string CleanFileName(string fileName)
        {
            fileName = fileName.Trim();

            char[] invalids = Path.GetInvalidFileNameChars();

            for (int i = 0; i < invalids.Length; i++)
            {
                char n = invalids[i];
                fileName = fileName.Replace(n.ToString(), string.Empty);
            }

            return fileName;
        }
    }
}
