using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Oxide.Core.Extensions
{
    /// <summary>
    /// Utility methods for extensions 
    /// </summary>
    public static class ExtensionUtility
    {
        /// <summary>
        /// Gets the base name of a file without extension
        /// </summary>
        /// <param name="path">The file path</param>
        /// <returns>The base name of the file without extension</returns>
        public static string Basename(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // If no extension, return the original path
            string extension = Path.GetExtension(path);
            if (string.IsNullOrEmpty(extension))
                return path;

            // Remove the extension from the filename
            return path.Substring(0, path.Length - extension.Length);
        }
    }
} 