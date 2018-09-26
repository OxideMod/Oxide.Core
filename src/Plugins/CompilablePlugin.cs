using System;
using System.Reflection;
using uMod.Plugins.Watchers;

namespace uMod.Plugins
{
    public class CompilablePlugin : CompilableFile
    {
        private static object compileLock = new object();

        public CompiledAssembly LastGoodAssembly;
        public bool IsLoading;

        public CompilablePlugin(CSharpPluginLoader loader, FSWatcher watcher, string directory, string name) : base(loader, watcher, directory, name)
        {
        }

        protected override void OnLoadingStarted() => Loader.PluginLoadingStarted(this);

        protected override void OnCompilationRequested() => Loader.CompilationRequested(this);

        internal void LoadPlugin(Action<CSharpPlugin> callback = null)
        {
            if (CompiledAssembly == null)
            {
                Interface.uMod.LogError($"Load called before a compiled assembly exists: {Name}");
                return;
            }

            LoadCallback = callback;

            CompiledAssembly.LoadAssembly(loaded =>
            {
                if (!loaded)
                {
                    callback?.Invoke(null);
                    return;
                }

                if (CompilerErrors != null)
                {
                    InitFailed($"Unable to load {ScriptName}. {CompilerErrors}");
                    return;
                }

                Type type = CompiledAssembly.LoadedAssembly.GetType($"uMod.Plugins.{Name}");
                if (type == null)
                {
                    InitFailed($"Unable to find main plugin class: {Name}");
                    return;
                }

                CSharpPlugin plugin;
                try
                {
                    plugin = Activator.CreateInstance(type) as CSharpPlugin;
                }
                catch (MissingMethodException)
                {
                    InitFailed($"Main plugin class should not have a constructor defined: {Name}");
                    return;
                }
                catch (TargetInvocationException invocationException)
                {
                    Exception ex = invocationException.InnerException;
                    InitFailed($"Unable to load {ScriptName}. {ex?.ToString()}");
                    return;
                }
                catch (Exception ex)
                {
                    InitFailed($"Unable to load {ScriptName}. {ex.ToString()}");
                    return;
                }

                if (plugin == null)
                {
                    InitFailed($"Plugin assembly failed to load: {ScriptName}");
                    return;
                }

                if (!plugin.SetPluginInfo(ScriptName, ScriptPath))
                {
                    InitFailed();
                    return;
                }

                plugin.Watcher = Watcher;
                plugin.Loader = Loader;

                if (!Interface.uMod.PluginLoaded(plugin))
                {
                    InitFailed();
                    return;
                }

                if (!CompiledAssembly.IsBatch)
                {
                    LastGoodAssembly = CompiledAssembly;
                }

                callback?.Invoke(plugin);
            });
        }

        internal override void OnCompilationStarted()
        {
            base.OnCompilationStarted();

            // Enqueue compilation of any plugins which depend on this plugin
            foreach (Plugin plugin in Interface.uMod.RootPluginManager.GetPlugins())
            {
                if (plugin is CSharpPlugin)
                {
                    CompilablePlugin compilablePlugin = CSharpPluginLoader.GetCompilablePlugin(Directory, plugin.Filename);
                    if (compilablePlugin.Requires.Contains(Name))
                    {
                        compilablePlugin.CompiledAssembly = null;
                        Loader.Load(compilablePlugin);
                    }
                }
            }
        }

        protected override void InitFailed(string message = null)
        {
            base.InitFailed(message);
            if (LastGoodAssembly == null)
            {
                Interface.uMod.LogInfo($"No previous version to rollback plugin: {ScriptName}");
                return;
            }

            if (CompiledAssembly == LastGoodAssembly)
            {
                Interface.uMod.LogInfo($"Previous version of plugin failed to load: {ScriptName}");
                return;
            }

            Interface.uMod.LogInfo($"Rolling back plugin to last good version: {ScriptName}");
            CompiledAssembly = LastGoodAssembly;
            CompilerErrors = null;
            LoadPlugin();
        }
    }
}
