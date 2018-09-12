using System;
using System.Collections.Generic;
using System.IO;

namespace uMod
{
    public static class Cleanup
    {
        internal static HashSet<string> files = new HashSet<string>();
        public static void Add(string file) => files.Add(file);

        internal static void Run()
        {
            if (files != null)
            {
                foreach (string file in files)
                {
                    try
                    {
                        if (File.Exists(file))
                        {
                            Interface.uMod.LogDebug($"Cleanup file: {file}");
                            File.Delete(file);
                        }
                    }
                    catch (Exception)
                    {
                        Interface.uMod.LogWarning($"Failed to cleanup file: {file}");
                    }
                }

                files = null;
            }
        }
    }
}
