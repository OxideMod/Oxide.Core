using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using uMod.Extensions;

namespace uMod.Plugins
{
    public class CSharpPluginLoader : PluginLoader
    {
        public static string[] DefaultReferences = { "mscorlib", "uMod", "System", "System.Core", "System.Data" };
        public static HashSet<string> PluginReferences = new HashSet<string>(DefaultReferences);
        public static CSharpPluginLoader Instance;

        private static CSharpExtension extension;
        private static Dictionary<string, CompilablePlugin> plugins = new Dictionary<string, CompilablePlugin>();
        private static readonly string[] AssemblyBlacklist = { "Newtonsoft.Json", "protobuf-net", "websocket-sharp" };

        public static CompilablePlugin GetCompilablePlugin(string directory, string name)
        {
            string className = Regex.Replace(name, "_", "");
            CompilablePlugin plugin;
            if (!plugins.TryGetValue(className, out plugin))
            {
                plugin = new CompilablePlugin(extension, Instance, directory, name);
                plugins[className] = plugin;
            }
            return plugin;
        }

        public override string FileExtension => ".cs";

        private List<CompilablePlugin> compilationQueue = new List<CompilablePlugin>();
        private PluginCompiler compiler;

        public CSharpPluginLoader(CSharpExtension extension)
        {
            Instance = this;
            CSharpPluginLoader.extension = extension;
            PluginCompiler.CheckCompilerBinary();
            compiler = new PluginCompiler();
        }

        public void OnModLoaded()
        {
            // Include references to all loaded game extensions and any assemblies they reference
            foreach (Extension extension in Interface.uMod.GetAllExtensions())
            {
                if (extension == null || !extension.IsCoreExtension && !extension.IsGameExtension)
                {
                    continue;
                }

                Assembly assembly = extension.GetType().Assembly;
                string assemblyName = assembly.GetName().Name;

                if (AssemblyBlacklist.Contains(assemblyName))
                {
                    continue;
                }

                PluginReferences.Add(assemblyName);
                foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
                {
                    if (reference != null)
                    {
                        PluginReferences.Add(reference.Name);
                    }
                }
            }
        }

        public override IEnumerable<string> ScanDirectory(string directory)
        {
            if (PluginCompiler.BinaryPath == null)
            {
                yield break;
            }

            IEnumerable<string> enumerable = base.ScanDirectory(directory);
            foreach (string file in enumerable)
            {
                yield return file;
            }
        }

        /// <summary>
        /// Attempt to asynchronously compile and load plugin
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public override Plugin Load(string directory, string name)
        {
            CompilablePlugin compilablePlugin = GetCompilablePlugin(directory, name);
            if (compilablePlugin.IsLoading)
            {
                Interface.uMod.LogDebug($"Load requested for plugin which is already loading: {compilablePlugin.Name}");
                return null;
            }

            // Attempt to compile the plugin before unloading the old version
            Load(compilablePlugin);

            return null;
        }

        /// <summary>
        /// Attempt to asynchronously compile plugin and only reload if successful
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="name"></param>
        public override void Reload(string directory, string name)
        {
            if (Regex.Match(directory, @"\\include\b", RegexOptions.IgnoreCase).Success)
            {
                name = $"uMod.{name}";
                foreach (CompilablePlugin plugin in plugins.Values)
                {
                    if (plugin.References.Contains(name))
                    {
                        Interface.uMod.LogInfo($"Reloading {plugin.Name} because it references updated include file: {name}");
                        plugin.LastModifiedAt = DateTime.Now;
                        Load(plugin);
                    }
                }
                return;
            }

            CompilablePlugin compilablePlugin = GetCompilablePlugin(directory, name);
            if (compilablePlugin.IsLoading)
            {
                Interface.uMod.LogDebug($"Reload requested for plugin which is already loading: {compilablePlugin.Name}");
                return;
            }

            // Attempt to compile the plugin before unloading the old version
            Load(compilablePlugin);
        }

        /// <summary>
        /// Called when the plugin manager is unloading a plugin that was loaded by this plugin loader
        /// </summary>
        /// <param name="pluginBase"></param>
        public override void Unloading(Plugin pluginBase)
        {
            CSharpPlugin plugin = pluginBase as CSharpPlugin;
            if (plugin != null)
            {
                LoadedPlugins.Remove(plugin.Name);

                // Unload plugins which require this plugin first
                foreach (CompilablePlugin compilablePlugin in plugins.Values)
                {
                    if (compilablePlugin.Requires.Contains(plugin.Name))
                    {
                        Interface.uMod.UnloadPlugin(compilablePlugin.Name);
                    }
                }
            }
        }

        public void Load(CompilablePlugin plugin)
        {
            plugin.Compile(compiled =>
            {
                if (!compiled)
                {
                    PluginLoadingCompleted(plugin);
                    return;
                }

                IEnumerable<string> loadedLoadingRequirements = plugin.Requires.Where(r => LoadedPlugins.ContainsKey(r) && LoadingPlugins.Contains(r));
                foreach (string loadedPlugin in loadedLoadingRequirements)
                {
                    Interface.uMod.UnloadPlugin(loadedPlugin);
                }

                IEnumerable<string> missingRequirements = plugin.Requires.Where(r => !LoadedPlugins.ContainsKey(r));
                if (missingRequirements.Any())
                {
                    IEnumerable<string> loadingRequirements = plugin.Requires.Where(r => LoadingPlugins.Contains(r));
                    if (loadingRequirements.Any())
                    {
                        Interface.uMod.LogDebug($"{plugin.Name} plugin is waiting for requirements to be loaded: {loadingRequirements.ToSentence()}");
                    }
                    else
                    {
                        Interface.uMod.LogError($"{plugin.Name} plugin requires missing dependencies: {missingRequirements.ToSentence()}");
                        PluginErrors[plugin.Name] = $"Missing dependencies: {missingRequirements.ToSentence()}";
                        PluginLoadingCompleted(plugin);
                    }
                }
                else
                {
                    Interface.uMod.UnloadPlugin(plugin.Name);
                    plugin.LoadPlugin(pl =>
                    {
                        if (pl != null)
                        {
                            LoadedPlugins[pl.Name] = pl;
                        }

                        PluginLoadingCompleted(plugin);
                    });
                }
            });
        }

        /// <summary>
        /// Called when a CompilablePlugin wants to be compiled
        /// </summary>
        /// <param name="plugin"></param>
        public void CompilationRequested(CompilablePlugin plugin)
        {
            if (Compilation.Current != null)
            {
                //Interface.uMod.LogDebug("Adding plugin to outstanding compilation: {0}", plugin.Name);
                Compilation.Current.Add(plugin);
                return;
            }

            if (compilationQueue.Count < 1)
            {
                Interface.uMod.NextTick(() =>
                {
                    CompileAssembly(compilationQueue.ToArray());
                    compilationQueue.Clear();
                });
            }
            compilationQueue.Add(plugin);
        }

        public void PluginLoadingStarted(CompilablePlugin plugin)
        {
            // Let the uMod know that this plugin will be loading asynchronously
            LoadingPlugins.Add(plugin.Name);
            plugin.IsLoading = true;
        }

        private void PluginLoadingCompleted(CompilablePlugin plugin)
        {
            LoadingPlugins.Remove(plugin.Name);
            plugin.IsLoading = false;
            foreach (string loadingName in LoadingPlugins.ToArray())
            {
                CompilablePlugin loadingPlugin = GetCompilablePlugin(plugin.Directory, loadingName);
                if (loadingPlugin.IsLoading && loadingPlugin.Requires.Contains(plugin.Name))
                {
                    Load(loadingPlugin);
                }
            }
        }

        private void CompileAssembly(CompilablePlugin[] plugins)
        {
            compiler.Compile(plugins, compilation =>
            {
                if (compilation.compiledAssembly == null)
                {
                    foreach (CompilablePlugin plugin in compilation.plugins)
                    {
                        plugin.OnCompilationFailed();
                        PluginErrors[plugin.Name] = $"Failed to compile: {plugin.CompilerErrors}";
                        Interface.uMod.LogError($"Error while compiling: {plugin.CompilerErrors}");
                        //RemoteLogger.Warning($"{plugin.ScriptName} plugin failed to compile!\n{plugin.CompilerErrors}");
                    }
                }
                else
                {
                    if (compilation.plugins.Count > 0)
                    {
                        string[] compiledNames = compilation.plugins.Where(pl => string.IsNullOrEmpty(pl.CompilerErrors)).Select(pl => pl.Name).ToArray();
                        string verb = compiledNames.Length > 1 ? "were" : "was";
                        Interface.uMod.LogInfo($"{compiledNames.ToSentence()} {verb} compiled successfully in {Math.Round(compilation.duration * 1000f)}ms");
                    }

                    foreach (CompilablePlugin plugin in compilation.plugins)
                    {
                        if (plugin.CompilerErrors == null)
                        {
                            Interface.uMod.UnloadPlugin(plugin.Name);
                            plugin.OnCompilationSucceeded(compilation.compiledAssembly);
                        }
                        else
                        {
                            plugin.OnCompilationFailed();
                            PluginErrors[plugin.Name] = $"Failed to compile: {plugin.CompilerErrors}";
                            Interface.uMod.LogError($"Error while compiling: {plugin.CompilerErrors}");
                        }
                    }
                }
            });
        }

        public void OnShutdown() => compiler.Shutdown();
    }
}
