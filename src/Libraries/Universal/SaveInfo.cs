using System;
using System.IO;

namespace uMod.Libraries.Universal
{
    public class SaveInfo
    {
        private readonly string FullPath;

        /// <summary>
        /// The name of the save file
        /// </summary>
        public string SaveName { get; private set; }

        /// <summary>
        /// Get the save creation time local to the server
        /// </summary>
        public DateTime CreationTime { get; private set; }

        /// <summary>
        /// Get the save creation time in Unix format
        /// </summary>
        public uint CreationTimeUnix { get; private set; }

        /// <summary>
        /// Refresh the save creation time
        /// </summary>
        public void Refresh()
        {
            if (File.Exists(FullPath))
            {
                CreationTime = File.GetCreationTime(FullPath);
                CreationTimeUnix = Utility.Time.ToTimestamp(CreationTime);
            }
        }

        private SaveInfo(string filePath)
        {
            FullPath = filePath;
            SaveName = Utility.GetFileNameWithoutExtension(filePath);
            Refresh();
        }

        /// <summary>
        /// Creates a new SaveInfo for a specifed file
        /// </summary>
        /// <param name="filePath">Full path to the save file</param>
        /// <returns></returns>
        public static SaveInfo Create(string filePath)
        {
            return !File.Exists(filePath) ? null : new SaveInfo(filePath);
        }
    }
}
