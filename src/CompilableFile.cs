using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace uMod.Plugins
{
    public class CompilableFile
    {
        private static Libraries.Timer timer = Interface.uMod.GetLibrary<Libraries.Timer>();
        private static object compileLock = new object();

        private Libraries.Timer.TimerInstance timeoutTimer;

        public CSharpPluginLoader Loader;
        public string Name;
        public string Directory;
        public string ScriptName;
        public string ScriptPath;
        public string[] ScriptLines;
        public Encoding ScriptEncoding;
        public HashSet<string> Requires = new HashSet<string>();
        public HashSet<string> References = new HashSet<string>();
        public HashSet<string> IncludePaths = new HashSet<string>();
        public string CompilerErrors;
        public CompiledAssembly CompiledAssembly;
        public DateTime LastModifiedAt;
        public DateTime LastCachedScriptAt;
        public DateTime LastCompiledAt;
        public bool IsCompilationNeeded;

        protected Action<CSharpPlugin> LoadCallback;
        protected Action<bool> CompileCallback;
        protected float CompilationQueuedAt;

        public byte[] ScriptSource => ScriptEncoding.GetBytes(string.Join(Environment.NewLine, ScriptLines));

        public CompilableFile(CSharpExtension extension, CSharpPluginLoader loader, string directory, string name)
        {
            Extension = extension;
            Loader = loader;
            Directory = directory;
            ScriptName = name;
            ScriptPath = Path.Combine(Directory, $"{ScriptName}.cs");
            Name = Regex.Replace(ScriptName, "_", "");
            CheckLastModificationTime();
        }

        internal void Compile(Action<bool> callback)
        {
            lock (compileLock)
            {
                if (CompilationQueuedAt > 0f)
                {
                    float ago = Interface.uMod.Now - CompilationQueuedAt;
                    Interface.uMod.LogDebug($"Plugin compilation is already queued: {ScriptName} ({ago:0.000} ago)");
                    //RemoteLogger.Debug($"Plugin compilation is already queued: {ScriptName} ({ago:0.000} ago)");
                    return;
                }

                OnLoadingStarted();
                if (CompiledAssembly != null && !HasBeenModified())
                {
                    if (CompiledAssembly.IsLoading || !CompiledAssembly.IsBatch || CompiledAssembly.CompilablePlugins.All(pl => pl.IsLoading))
                    {
                        //Interface.uMod.LogDebug("Plugin is already compiled: {0}", Name);
                        callback(true);
                        return;
                    }
                }

                IsCompilationNeeded = true;
                CompileCallback = callback;
                CompilationQueuedAt = Interface.uMod.Now;
                OnCompilationRequested();
            }
        }

        internal virtual void OnCompilationStarted()
        {
            //Interface.uMod.LogDebug("Compiling plugin: {0}", Name);
            LastCompiledAt = LastModifiedAt;
            timeoutTimer?.Destroy();
            timeoutTimer = null;
            Interface.uMod.NextTick(() =>
            {
                timeoutTimer?.Destroy();
                timeoutTimer = timer.Once(60f, OnCompilationTimeout);
            });
        }

        internal void OnCompilationSucceeded(CompiledAssembly compiledAssembly)
        {
            if (timeoutTimer == null)
            {
                Interface.uMod.LogWarning($"Ignored unexpected plugin compilation: {Name}");
                return;
            }

            timeoutTimer?.Destroy();
            timeoutTimer = null;
            IsCompilationNeeded = false;
            CompilationQueuedAt = 0f;
            CompiledAssembly = compiledAssembly;
            CompileCallback?.Invoke(true);
        }

        internal void OnCompilationFailed()
        {
            if (timeoutTimer == null)
            {
                Interface.uMod.LogWarning($"Ignored unexpected plugin compilation failure: {Name}");
                return;
            }

            timeoutTimer?.Destroy();
            timeoutTimer = null;
            CompilationQueuedAt = 0f;
            LastCompiledAt = default(DateTime);
            CompileCallback?.Invoke(false);
            IsCompilationNeeded = false;
        }

        internal void OnCompilationTimeout()
        {
            Interface.uMod.LogError("Timed out waiting for plugin to be compiled: " + Name);
            CompilerErrors = "Timed out waiting for compilation";
            OnCompilationFailed();
        }

        internal bool HasBeenModified()
        {
            DateTime lastModifiedAt = LastModifiedAt;
            CheckLastModificationTime();
            return LastModifiedAt != lastModifiedAt;
        }

        internal void CheckLastModificationTime()
        {
            if (!File.Exists(ScriptPath))
            {
                LastModifiedAt = default(DateTime);
                return;
            }

            DateTime modifiedTime = GetLastModificationTime();
            if (modifiedTime != default(DateTime))
            {
                LastModifiedAt = modifiedTime;
            }
        }

        internal DateTime GetLastModificationTime()
        {
            try
            {
                return File.GetLastWriteTime(ScriptPath);
            }
            catch (IOException ex)
            {
                Interface.uMod.LogError("IOException while checking plugin: {0} ({1})", ScriptName, ex.Message);
                return default(DateTime);
            }
        }

        protected virtual void OnLoadingStarted()
        {
        }

        protected virtual void OnCompilationRequested()
        {
        }

        protected virtual void InitFailed(string message = null)
        {
            if (message != null)
            {
                Interface.uMod.LogError(message);
            }

            LoadCallback?.Invoke(null);
        }
    }
}
