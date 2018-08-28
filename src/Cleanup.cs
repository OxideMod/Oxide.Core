using System;
using System.Collections.Generic;
using System.IO;

namespace Umod
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
                            Interface.Umod.LogDebug("Cleanup file: {0}", file);
                            File.Delete(file);
                        }
                    }
                    catch (Exception)
                    {
                        Interface.Umod.LogWarning("Failed to cleanup file: {0}", file);
                    }
                }

                files = null;
            }
        }
    }
}
