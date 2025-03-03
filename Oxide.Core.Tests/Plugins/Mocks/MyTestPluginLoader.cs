using System.IO;
using System.Linq;
using Oxide.Core.Plugins;
using System.Collections.Generic;

namespace Oxide.Core.Tests.Plugins.Mocks
{
    /// <summary>
    /// A plugin loader for testing purposes.
    /// </summary>
    public class MyTestPluginLoader : PluginLoader
    {
        public override string FileExtension => ".cs";

        public override IEnumerable<string> ScanDirectory(string directory)
        {
            if (!Directory.Exists(directory))
                yield break;

            var files = new DirectoryInfo(directory).GetFiles($"*{FileExtension}");
            foreach (var file in files)
            {
                yield return Path.GetFileNameWithoutExtension(file.Name);
            }
        }

        // Load a plugin from the specified directory and name.
        public override Plugin Load(string directory, string name)
        {
            // For testing, simply return a new FakePlugin with the specified name.
            var plugin = new FakePlugin
            {
                Name = name
            };
            plugin.Load();
            return plugin;
        }
    }
}
