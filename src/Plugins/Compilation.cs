using ObjectStream.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using uMod.Extensions;

namespace uMod.Plugins
{
    class Compilation
    {
        public static Compilation Current;

        internal int id;
        internal string name;
        internal Action<Compilation> callback;
        internal ConcurrentHashSet<CompilablePlugin> queuedPlugins;
        internal HashSet<CompilablePlugin> plugins = new HashSet<CompilablePlugin>();
        internal float startedAt;
        internal float endedAt;
        internal Hash<string, CompilerFile> references = new Hash<string, CompilerFile>();
        internal HashSet<string> referencedPlugins = new HashSet<string>();
        internal CompiledAssembly compiledAssembly;
        internal float duration => endedAt - startedAt;

        private readonly string[] extensionNames;
        private readonly string includePath;
        private readonly string gameExtensionName;
        private readonly string gameExtensionBranch;

        internal Compilation(int id, Action<Compilation> callback, CompilablePlugin[] plugins)
        {
            this.id = id;
            this.callback = callback;
            this.queuedPlugins = new ConcurrentHashSet<CompilablePlugin>(plugins);

            if (Current == null)
            {
                Current = this;
            }

            foreach (CompilablePlugin plugin in plugins)
            {
                plugin.CompilerErrors = null;
                plugin.OnCompilationStarted();
            }

            includePath = Path.Combine(Interface.uMod.PluginDirectory, "include");
            extensionNames = Interface.uMod.GetAllExtensions().Select(ext => ext.Name).ToArray();
            Extension gameExtension = Interface.uMod.GetAllExtensions().SingleOrDefault(ext => ext.IsGameExtension);
            gameExtensionName = gameExtension?.Name.ToUpper();
            gameExtensionBranch = gameExtension?.Branch?.ToUpper();
        }

        internal void Started()
        {
            startedAt = Interface.uMod.Now;
            name = (plugins.Count < 2 ? plugins.First().Name : "plugins_") + Math.Round(Interface.uMod.Now * 10000000f) + ".dll";
        }

        internal void Completed(byte[] rawAssembly = null)
        {
            endedAt = Interface.uMod.Now;
            if (plugins.Count > 0 && rawAssembly != null)
            {
                compiledAssembly = new CompiledAssembly(name, plugins.ToArray(), rawAssembly, duration);
            }
            Interface.uMod.NextTick(() => callback(this));
        }

        internal void Add(CompilablePlugin plugin)
        {
            if (queuedPlugins.Add(plugin))
            {
                plugin.Loader.PluginLoadingStarted(plugin);
                plugin.CompilerErrors = null;
                plugin.OnCompilationStarted();

                foreach (Plugin pl in Interface.uMod.RootPluginManager.GetPlugins().Where(pl => pl is CSharpPlugin))
                {
                    CompilablePlugin loadedPlugin = CSharpPluginLoader.GetCompilablePlugin(plugin.Directory, pl.Filename);
                    if (loadedPlugin.Requires.Contains(plugin.Name))
                    {
                        AddDependency(loadedPlugin);
                    }
                }
            }
        }

        internal bool IncludesRequiredPlugin(string name)
        {
            if (!referencedPlugins.Contains(name))
            {
                CompilablePlugin compilablePlugin = plugins.SingleOrDefault(pl => pl.Name == name);
                return compilablePlugin != null && compilablePlugin.CompilerErrors == null;
            }

            return true;
        }

        internal void Prepare(Action callback)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    referencedPlugins.Clear();
                    references.Clear();

                    // Include references made by the CSharpPlugins project
                    foreach (string filename in CSharpPluginLoader.PluginReferences)
                    {
                        if (File.Exists(Path.Combine(Interface.uMod.ExtensionDirectory, filename + ".dll")))
                        {
                            references[filename + ".dll"] = new CompilerFile(Interface.uMod.ExtensionDirectory, filename + ".dll");
                        }

                        if (File.Exists(Path.Combine(Interface.uMod.RootDirectory, filename + ".exe")))
                        {
                            references[filename + ".exe"] = new CompilerFile(Interface.uMod.RootDirectory, filename + ".exe");
                        }
                    }

                    while (queuedPlugins.TryDequeue(out CompilablePlugin plugin))
                    {
                        if (Current == null)
                        {
                            Current = this;
                        }

                        if (!CacheScriptLines(plugin) || plugin.ScriptLines.Length < 1)
                        {
                            plugin.References.Clear();
                            plugin.Requires.Clear();
                            plugin.IncludePaths.Clear();
                            Interface.uMod.LogWarning("Plugin script is empty: " + plugin.Name);
                            RemovePlugin(plugin);
                        }
                        else if (plugins.Add(plugin))
                        {
                            PreparseScript(plugin);
                            ResolveReferences(plugin);
                        }

                        CacheModifiedScripts();

                        // We don't want the main thread to be able to add more plugins which could be missed
                        if (queuedPlugins.Count == 0 && Current == this)
                        {
                            Current = null;
                        }
                    }

                    callback();
                }
                catch (Exception ex)
                {
                    Interface.uMod.LogException("Exception while resolving plugin references", ex);
                }
            });
        }

        private void PreparseScript(CompilablePlugin plugin)
        {
            plugin.References.Clear();
            plugin.Requires.Clear();
            plugin.IncludePaths.Clear();

            // Try to provide at least some deprecation for the rename
            if (plugin.ScriptLines.Any(line => line.Contains("Oxide") || line.Contains("Covalence")))
            {
                plugin.ScriptLines = plugin.ScriptLines.Select(s => s
                    .Replace("Oxide.Core", "uMod")
                    .Replace("OxideMod", "uMod")
                    .Replace("Oxide", "uMod")
                    .Replace("Covalence", "Universal"))
                    .ToArray();
                Interface.uMod.LogWarning($"Plugin {plugin.ScriptName} is using Oxide naming, please update to uMod naming");
            }

            bool parsingNamespace = false;
            for (int i = 0; i < plugin.ScriptLines.Length; i++)
            {
                // Skip empty files, they aren't plugins
                string line = plugin.ScriptLines[i].Trim();
                if (line.Length < 1)
                {
                    continue;
                }

                Match match;
                if (parsingNamespace)
                {
                    // Skip blank lines and opening brace at the top of the namespace block
                    match = Regex.Match(line, @"^\s*\{?\s*$", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        continue;
                    }

                    // Skip class custom attributes
                    match = Regex.Match(line, @"^\s*\[", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        continue;
                    }

                    // Detect main plugin class name
                    match = Regex.Match(line, @"^\s*(?:public|private|protected|internal)?\s*class\s+(\S+)\s+\:\s+\S+Plugin\s*$", RegexOptions.IgnoreCase);
                    if (!match.Success)
                    {
                        break;
                    }

                    string className = match.Groups[1].Value;
                    if (className != plugin.Name)
                    {
                        Interface.uMod.LogError($"Plugin filename {plugin.ScriptName}.cs must match the main class {className} (should be {className}.cs)");
                        plugin.CompilerErrors = $"Plugin filename {plugin.ScriptName}.cs must match the main class {className} (should be {className}.cs)";
                        RemovePlugin(plugin);
                    }

                    break;
                }

                // Include explicit plugin dependencies defined by magic comments in script
                match = Regex.Match(line, @"^//\s*Requires:\s*(\S+?)(\.cs)?\s*$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string dependencyName = match.Groups[1].Value;
                    plugin.Requires.Add(dependencyName);

                    FileInfo dependency = plugin.Loader.ScanDirectory(Interface.uMod.PluginDirectory).First(f => f.Name.StartsWith(dependencyName));
                    if (dependency == null)
                    {
                        Interface.uMod.LogError($"Plugin '{plugin.Name}' requires missing dependency: {dependencyName}");
                        plugin.CompilerErrors = $"Missing dependency: {dependencyName}";
                        RemovePlugin(plugin);
                        return;
                    }

                    FileInfo dependency = plugin.Loader.ScanDirectory(Interface.uMod.PluginDirectory).First(f => f.Name.StartsWith(dependencyName));
                    CompilablePlugin dependencyPlugin = CSharpPluginLoader.GetCompilablePlugin(dependency.DirectoryName, dependencyName);
                    AddDependency(dependencyPlugin);
                    continue;
                }

                // Include explicit references defined by magic comments in script
                match = Regex.Match(line, @"^//\s*Reference:\s*(\S+)\s*$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string result = match.Groups[1].Value;
                    if (!result.StartsWith("uMod.") && !result.StartsWith("Newtonsoft.Json") && !result.StartsWith("protobuf-net") && !result.StartsWith("Rust."))
                    {
                        AddReference(plugin, result);
                        Interface.uMod.LogInfo("Added '// Reference: {0}' in plugin '{1}'", result, plugin.Name);
                    }
                    else
                    {
                        Interface.uMod.LogWarning("Ignored unnecessary '// Reference: {0}' in plugin '{1}'", result, plugin.Name);
                    }

                    continue;
                }

                // Start parsing the uMod.Plugins namespace contents
                match = Regex.Match(line, @"^\s*namespace (Oxide|uMod)\.Plugins\s*(\{\s*)?$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    parsingNamespace = true;
                }
            }
        }

        private void ResolveReferences(CompilablePlugin plugin)
        {
            foreach (string reference in plugin.References)
            {
                Match match = Regex.Match(reference, @"^((Oxide|uMod)\.(?:Ext|Game)\.(.+))$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string fullName = match.Groups[1].Value;
                    string name = match.Groups[2].Value;
                    if (!extensionNames.Contains(name))
                    {
                        if (Directory.Exists(includePath))
                        {
                            string includeFilePath = Path.Combine(includePath, $"{name}.cs");
                            if (File.Exists(includeFilePath))
                            {
                                plugin.IncludePaths.Add(includeFilePath);
                                continue;
                            }
                        }

                        string message = $"{fullName} is referenced by {plugin.Name} plugin but is not loaded!";
                        Interface.uMod.LogError(message);
                        plugin.CompilerErrors = message;
                        RemovePlugin(plugin);
                    }
                }
            }
        }

        private void AddDependency(CompilablePlugin plugin)
        {
            if (plugin.IsLoading || plugins.Contains(plugin) || queuedPlugins.Contains(plugin))
            {
                return;
            }

            CompiledAssembly compiledDependency = plugin.CompiledAssembly;
            if (compiledDependency != null && !compiledDependency.IsOutdated())
            {
                // The dependency already has a compiled assembly which is up to date
                referencedPlugins.Add(plugin.Name);
                if (!references.ContainsKey(compiledDependency.Name))
                {
                    references[compiledDependency.Name] = new CompilerFile(compiledDependency.Name, compiledDependency.RawAssembly);
                }
            }
            else
            {
                // The dependency needs to be compiled
                Add(plugin);
            }
        }

        private void AddReference(CompilablePlugin plugin, string assemblyName)
        {
            string path = Path.Combine(Interface.uMod.ExtensionDirectory, assemblyName + ".dll");
            if (!File.Exists(path))
            {
                if (assemblyName.StartsWith("uMod."))
                {
                    plugin.References.Add(assemblyName);
                    return;
                }

                Interface.uMod.LogError($"Could not find assembly for {assemblyName} referenced by {plugin.Name} plugin");
                plugin.CompilerErrors = $"Referenced assembly does not exist: {assemblyName}";
                RemovePlugin(plugin);
                return;
            }

            Assembly assembly;
            try
            {
                assembly = Assembly.Load(assemblyName);
            }
            catch (FileNotFoundException)
            {
                Interface.uMod.LogError($"Assembly referenced by {plugin.Name} plugin is invalid: {assemblyName}");
                plugin.CompilerErrors = $"Referenced assembly is invalid: {assemblyName}";
                RemovePlugin(plugin);
                return;
            }

            AddReference(plugin, assembly.GetName());

            // Include references made by the referenced assembly
            foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
            {
                // TODO: Fix Oxide.References to avoid these and other dependency conflicts
                if (reference.Name.StartsWith("Newtonsoft.Json") || reference.Name.StartsWith("Rust.Workshop"))
                {
                    continue;
                }

                string referencePath = Path.Combine(Interface.uMod.ExtensionDirectory, reference.Name + ".dll");
                if (!File.Exists(referencePath))
                {
                    Interface.uMod.LogWarning($"Reference {reference.Name}.dll from {assembly.GetName().Name}.dll not found");
                    continue;
                }

                AddReference(plugin, reference);
            }
        }

        private void AddReference(CompilablePlugin plugin, AssemblyName reference)
        {
            string filename = reference.Name + ".dll";
            if (!references.ContainsKey(filename))
            {
                references[filename] = new CompilerFile(Interface.uMod.ExtensionDirectory, filename);
            }
            plugin.References.Add(reference.Name);
        }

        private bool CacheScriptLines(CompilablePlugin plugin)
        {
            bool waitingForAccess = false;
            while (true)
            {
                try
                {
                    if (!File.Exists(plugin.ScriptPath))
                    {
                        Interface.uMod.LogWarning($"Script no longer exists: {plugin.Name}");
                        plugin.CompilerErrors = "Plugin file was deleted";
                        RemovePlugin(plugin);
                        return false;
                    }

                    plugin.CheckLastModificationTime();
                    if (plugin.LastCachedScriptAt != plugin.LastModifiedAt)
                    {
                        using (StreamReader reader = File.OpenText(plugin.ScriptPath))
                        {
                            List<string> lines = new List<string>();
                            while (!reader.EndOfStream)
                            {
                                lines.Add(reader.ReadLine());
                            }

                            if (!string.IsNullOrEmpty(gameExtensionName))
                            {
                                lines.Insert(0, $"#define {gameExtensionName}");

                                if (!string.IsNullOrEmpty(gameExtensionBranch) && gameExtensionBranch != "public")
                                {
                                    lines.Insert(0, $"#define {gameExtensionName}{gameExtensionBranch}");
                                }
                            }

                            plugin.ScriptLines = lines.ToArray();
                            plugin.ScriptEncoding = reader.CurrentEncoding;
                        }
                        plugin.LastCachedScriptAt = plugin.LastModifiedAt;
                        if (plugins.Remove(plugin))
                        {
                            queuedPlugins.Add(plugin);
                        }
                    }
                    return true;
                }
                catch (IOException)
                {
                    if (!waitingForAccess)
                    {
                        waitingForAccess = true;
                        Interface.uMod.LogWarning($"Waiting for another application to stop using script: {plugin.Name}");
                    }
                    Thread.Sleep(50);
                }
            }
        }

        private void CacheModifiedScripts()
        {
            CompilablePlugin[] modifiedPlugins = plugins.Where(pl => pl.ScriptLines == null || pl.HasBeenModified() || pl.LastCachedScriptAt != pl.LastModifiedAt).ToArray();
            if (modifiedPlugins.Length >= 1)
            {
                foreach (CompilablePlugin plugin in modifiedPlugins)
                {
                    CacheScriptLines(plugin);
                }

                Thread.Sleep(100);
                CacheModifiedScripts();
            }
        }

        private void RemovePlugin(CompilablePlugin plugin)
        {
            if (plugin.LastCompiledAt != default(DateTime))
            {
                queuedPlugins.Remove(plugin);
                plugins.Remove(plugin);
                plugin.OnCompilationFailed();

                // Remove plugins which are required by this plugin if they are only being compiled for this requirement
                foreach (CompilablePlugin requiredPlugin in plugins.Where(pl => !pl.IsCompilationNeeded && plugin.Requires.Contains(pl.Name)).ToArray())
                {
                    if (!plugins.Any(pl => pl.Requires.Contains(requiredPlugin.Name)))
                    {
                        RemovePlugin(requiredPlugin);
                    }
                }
            }
        }
    }
}
