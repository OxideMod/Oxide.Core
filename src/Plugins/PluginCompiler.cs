extern alias References;

using ObjectStream;
using ObjectStream.Data;
using Rebex.Net;
using Rebex.Security.Cryptography;
using References::Mono.Unix;
using References::Mono.Unix.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using uMod.Logging;

namespace uMod.Plugins
{
    public class PluginCompiler
    {
        public static bool AutoShutdown = true;
        public static bool TraceRan;
        public static string FileName = "mcs.exe";
        public static string BinaryPath;
        public static string CompilerVersion;

        private static int downloadRetries;

        public static void CheckCompilerBinary()
        {
            BinaryPath = null;
            string rootDirectory = Interface.uMod.RootDirectory;
            string binaryPath = Path.Combine(rootDirectory, FileName);

            if (File.Exists(binaryPath))
            {
                BinaryPath = binaryPath;
                return;
            }

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                    FileName = "Compiler.exe";
                    binaryPath = Path.Combine(rootDirectory, FileName);
                    UpdateCheck(); // TODO: Only check once on server startup
                    break;

                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    FileName = $"Compiler.{(IntPtr.Size != 8 ? "x86" : "x86_x64")}";
                    binaryPath = Path.Combine(rootDirectory, FileName);
                    UpdateCheck(); // TODO: Only check once on server startup
                    try
                    {
                        if (Syscall.access(binaryPath, AccessModes.X_OK) == 0)
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Interface.uMod.LogError($"Unable to check {FileName} for executable permission");
                        Interface.uMod.LogError(ex.Message);
                        Interface.uMod.LogError(ex.StackTrace);
                    }
                    try
                    {
                        Syscall.chmod(binaryPath, FilePermissions.S_IRWXU);
                    }
                    catch (Exception ex)
                    {
                        Interface.uMod.LogError($"Could not set {FileName} as executable, please set manually");
                        Interface.uMod.LogError(ex.Message);
                        Interface.uMod.LogError(ex.StackTrace);
                    }
                    break;
            }
            BinaryPath = binaryPath;
        }

        private void DependencyTrace()
        {
            if (TraceRan || Environment.OSVersion.Platform != PlatformID.Unix)
            {
                return;
            }

            try
            {
                Interface.uMod.LogWarning($"Running dependency trace for {FileName}");
                Process trace = new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = Interface.uMod.RootDirectory,
                        FileName = "/bin/bash",
                        Arguments = $"-c \"LD_TRACE_LOADED_OBJECTS=1 {BinaryPath}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true
                    },
                    EnableRaisingEvents = true
                };
                string unixPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH");
                Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", unixPath + $":{Path.Combine(Interface.uMod.ExtensionDirectory, IntPtr.Size == 8 ? "x64" : "x86")}");
                trace.StartInfo.EnvironmentVariables["LD_LIBRARY_PATH"] = Path.Combine(Interface.uMod.ExtensionDirectory, IntPtr.Size == 8 ? "x64" : "x86");
                trace.ErrorDataReceived += (s, e) => Interface.uMod.LogError(e.Data.TrimStart());
                trace.OutputDataReceived += (s, e) => Interface.uMod.LogError(e.Data.TrimStart());
                trace.Start();
                trace.BeginOutputReadLine();
                trace.BeginErrorReadLine();
                trace.WaitForExit();
            }
            catch (Exception)
            {
                //Interface.uMod.LogError($"Couldn't run dependency trace"); // TODO: Fix this triggering sometimes
                //Interface.uMod.LogError(ex.Message);
            }
            TraceRan = true;
        }

        private static void DownloadCompiler(WebResponse response, string remoteHash)
        {
            try
            {
                Interface.uMod.LogInfo($"Downloading {FileName} for .cs (C#) plugin compilation");

                Stream stream = response.GetResponseStream();
                FileStream fs = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.None);
                int bufferSize = 10000;
                byte[] buffer = new byte[bufferSize];

                while (true)
                {
                    int result = stream.Read(buffer, 0, bufferSize);
                    if (result == -1 || result == 0)
                    {
                        break;
                    }

                    fs.Write(buffer, 0, result);
                }
                fs.Flush();
                fs.Close();
                stream.Close();
                response.Close();

                if (downloadRetries >= 3)
                {
                    Interface.uMod.LogInfo($"Couldn't download {FileName}! Please download manually from: https://github.com/theumod/Compiler/releases/download/latest/{FileName}");
                    return;
                }

                string localHash = File.Exists(BinaryPath) ? GetHash(BinaryPath, Algorithms.MD5) : "0";
                if (remoteHash != localHash)
                {
                    Interface.uMod.LogInfo($"Local hash did not match remote hash for {FileName}, attempting download again");
                    UpdateCheck();
                    downloadRetries++;
                    return;
                }

                Interface.uMod.LogInfo($"Download of {FileName} completed successfully");
            }
            catch (Exception ex)
            {
                Interface.uMod.LogError($"Couldn't download {FileName}! Please download manually from: https://github.com/theumod/Compiler/releases/download/latest/{FileName}");
                Interface.uMod.LogError(ex.Message);
            }
        }

        private static void UpdateCheck()
        {
            try
            {
                Rebex.Licensing.Key = "==ArpOkr5jPVQy0BbM9N19ePmdqRP+bMGL9yK5/F/UPWRU=="; // Expires: 12-27-18, TODO: Obfuscate production key

                // Override the web request creator
                HttpRequestCreator creator = new HttpRequestCreator();
                creator.Register();

                // Import NIST and Brainpool curves crypto
                AsymmetricKeyAlgorithm.Register(EllipticCurveAlgorithm.Create);

                // Import Curve25519 crypto
                AsymmetricKeyAlgorithm.Register(Curve25519.Create);

                // Import Ed25519 crypto
                AsymmetricKeyAlgorithm.Register(Ed25519.Create);

                // Override certificate validation (necessary for Mono)
                //creator.ValidatingCertificate += ValidatingCertificate; // TODO: Use this when ECDSA issue is resolved in dep update
                creator.ValidatingCertificate += (sender, args) => args.Accept();

                // Create the web request
                HttpRequest request = creator.Create($"https://github.com/theumod/Compiler/releases/download/latest/{FileName}");

                string filePath = Path.Combine(Interface.uMod.RootDirectory, FileName);
                HttpResponse response = (HttpResponse)request.GetResponse();
                HttpStatusCode statusCode = response.StatusCode;
                if (statusCode != HttpStatusCode.OK)
                {
                    Interface.uMod.LogWarning($"Status code from download location was not okay (code {statusCode})");
                }

                string remoteHash = response.Headers[HttpResponseHeader.ETag].Trim('"');
                string localHash = File.Exists(filePath) ? GetHash(filePath, Algorithms.MD5) : "0";
                Interface.uMod.LogInfo($"Latest compiler MD5: {remoteHash}");
                Interface.uMod.LogInfo($"Local compiler MD5: {localHash}");
                if (remoteHash != localHash)
                {
                    Interface.uMod.LogInfo("Compiler hashes did not match, downloading latest");
                    DownloadCompiler(response, remoteHash);
                }
            }
            catch (Exception ex)
            {
                Interface.uMod.LogError($"Couldn't check for update to {FileName}");
                Interface.uMod.LogError(ex.Message);
            }
        }

        private static void SetCompilerVersion()
        {
            CompilerVersion = File.Exists(BinaryPath) ? FileVersionInfo.GetVersionInfo(BinaryPath).FileVersion : "Unknown";
            RemoteLogger.SetTag("compiler version", CompilerVersion);
        }

        private Process process;
        private readonly Regex fileErrorRegex = new Regex(@"([\w\.]+)\(\d+\,\d+\+?\): error|error \w+: Source file `[\\\./]*([\w\.]+)", RegexOptions.Compiled);
        private ObjectStreamClient<CompilerMessage> client;
        private Hash<int, Compilation> compilations;
        private Queue<CompilerMessage> messageQueue;
        private volatile int lastId;
        private volatile bool ready;
        private Libraries.Timer.TimerInstance idleTimer;

        public PluginCompiler()
        {
            compilations = new Hash<int, Compilation>();
            messageQueue = new Queue<CompilerMessage>();
        }

        internal void Compile(CompilablePlugin[] plugins, Action<Compilation> callback)
        {
            int id = lastId++;
            Compilation compilation = new Compilation(id, callback, plugins);
            compilations[id] = compilation;
            compilation.Prepare(() => EnqueueCompilation(compilation));
        }

        public void Shutdown()
        {
            ready = false;
            Process endedProcess = process;
            if (endedProcess != null)
            {
                endedProcess.Exited -= OnProcessExited;
            }

            process = null;
            if (client == null)
            {
                return;
            }

            client.Message -= OnMessage;
            client.Error -= OnError;
            client.PushMessage(new CompilerMessage { Type = CompilerMessageType.Exit });
            client.Stop();
            client = null;
            if (endedProcess == null)
            {
                return;
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(5000);
                // Calling Close can block up to 60 seconds on certain machines
                if (!endedProcess.HasExited)
                {
                    endedProcess.Close();
                }
            });
        }

        private void EnqueueCompilation(Compilation compilation)
        {
            if (compilation.plugins.Count < 1)
            {
                //Interface.uMod.LogDebug("EnqueueCompilation called for an empty compilation");
                return;
            }

            if (!CheckCompiler())
            {
                OnCompilerFailed($"compiler version {CompilerVersion} couldn't be started");
                return;
            }

            compilation.Started();
            //Interface.uMod.LogDebug("Compiling with references: {0}", compilation.references.Keys.ToSentence());
            List<CompilerFile> sourceFiles = compilation.plugins.SelectMany(plugin => plugin.IncludePaths).Distinct().Select(path => new CompilerFile(path)).ToList();
            sourceFiles.AddRange(compilation.plugins.Select(plugin => new CompilerFile($"{plugin.ScriptName}.cs", plugin.ScriptSource)));
            //Interface.uMod.LogDebug("Compiling files: {0}", sourceFiles.Select(f => f.Name).ToSentence());
            CompilerData data = new CompilerData
            {
                OutputFile = compilation.name,
                SourceFiles = sourceFiles.ToArray(),
                ReferenceFiles = compilation.references.Values.ToArray()
            };
            CompilerMessage message = new CompilerMessage { Id = compilation.id, Data = data, Type = CompilerMessageType.Compile };

            if (ready)
            {
                client.PushMessage(message);
            }
            else
            {
                messageQueue.Enqueue(message);
            }
        }

        private void OnMessage(ObjectStreamConnection<CompilerMessage, CompilerMessage> connection, CompilerMessage message)
        {
            if (message == null)
            {
                Interface.uMod.NextTick(() =>
                {
                    OnCompilerFailed($"compiler version {CompilerVersion} disconnected");
                    DependencyTrace();
                    Shutdown();
                });
                return;
            }

            switch (message.Type)
            {
                case CompilerMessageType.Assembly:
                    Compilation compilation = compilations[message.Id];

                    if (compilation == null)
                    {
                        Interface.uMod.LogWarning("Compiler compiled an unknown assembly"); // TODO: Any way to clarify this?
                        return;
                    }

                    compilation.endedAt = Interface.uMod.Now;
                    string stdOutput = (string)message.ExtraData;
                    if (stdOutput != null)
                    {
                        foreach (string line in stdOutput.Split('\r', '\n'))
                        {
                            Match match = fileErrorRegex.Match(line.Trim());
                            for (int i = 1; i < match.Groups.Count; i++)
                            {
                                string value = match.Groups[i].Value;

                                if (value.Trim() == string.Empty)
                                {
                                    continue;
                                }

                                string fileName = value.Basename();
                                string scriptName = fileName.Substring(0, fileName.Length - 3);
                                CompilablePlugin compilablePlugin = compilation.plugins.SingleOrDefault(pl => pl.ScriptName == scriptName);

                                if (compilablePlugin == null)
                                {
                                    Interface.uMod.LogError($"Unable to resolve script error to plugin: {line}");
                                    continue;
                                }

                                IEnumerable<string> missingRequirements = compilablePlugin.Requires.Where(name => !compilation.IncludesRequiredPlugin(name));
                                if (missingRequirements.Any())
                                {
                                    compilablePlugin.CompilerErrors = $"Missing dependencies: {missingRequirements.ToSentence()}";
                                }
                                else
                                {
                                    compilablePlugin.CompilerErrors = line.Trim().Replace(Interface.uMod.PluginDirectory + Path.DirectorySeparatorChar, string.Empty);
                                }
                            }
                        }
                    }
                    compilation.Completed((byte[])message.Data);
                    compilations.Remove(message.Id);
                    idleTimer?.Destroy();
                    if (AutoShutdown)
                    {
                        Interface.uMod.NextTick(() =>
                        {
                            idleTimer?.Destroy();
                            if (AutoShutdown)
                            {
                                idleTimer = Interface.uMod.GetLibrary<Libraries.Timer>().Once(60, Shutdown);
                            }
                        });
                    }
                    break;

                case CompilerMessageType.Error:
                    Interface.uMod.LogError("Compilation error: {0}", message.Data);
                    compilations[message.Id].Completed();
                    compilations.Remove(message.Id);
                    idleTimer?.Destroy();
                    if (AutoShutdown)
                    {
                        Interface.uMod.NextTick(() =>
                        {
                            idleTimer?.Destroy();
                            idleTimer = Interface.uMod.GetLibrary<Libraries.Timer>().Once(60, Shutdown);
                        });
                    }
                    break;

                case CompilerMessageType.Ready:
                    connection.PushMessage(message);
                    if (!ready)
                    {
                        ready = true;
                        while (messageQueue.Count > 0)
                        {
                            connection.PushMessage(messageQueue.Dequeue());
                        }
                    }
                    break;
            }
        }

        private static void OnError(Exception exception) => Interface.uMod.LogException("Compilation error: ", exception);

        private bool CheckCompiler()
        {
            CheckCompilerBinary();
            idleTimer?.Destroy();

            if (BinaryPath == null)
            {
                return false;
            }

            if (process != null && process.Handle != IntPtr.Zero && !process.HasExited)
            {
                return true;
            }

            SetCompilerVersion();
            PurgeOldLogs();
            Shutdown();

            string[] args = { "/service", "/logPath:" + EscapePath(Interface.uMod.LogDirectory) }; // TODO: Handle exception if log path isn't accessible?
            try
            {
                process = new Process
                {
                    StartInfo =
                    {
                        FileName = BinaryPath,
                        Arguments = string.Join(" ", args),
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true
                    },
                    EnableRaisingEvents = true
                };
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Win32NT:
                    case PlatformID.Win32S:
                    case PlatformID.Win32Windows:
                        string winPath = Environment.GetEnvironmentVariable("PATH");
                        //process.StartInfo.EnvironmentVariables["PATH"] = Path.Combine(Interface.uMod.ExtensionDirectory, "x86"); // Not working
                        Environment.SetEnvironmentVariable("PATH", winPath + $";{Path.Combine(Interface.uMod.ExtensionDirectory, "x86")}");
                        break;

                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        string unixPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH");
                        process.StartInfo.EnvironmentVariables["LD_LIBRARY_PATH"] = Path.Combine(Interface.uMod.ExtensionDirectory, IntPtr.Size == 8 ? "x64" : "x86");
                        Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", unixPath + $":{Path.Combine(Interface.uMod.ExtensionDirectory, IntPtr.Size == 8 ? "x64" : "x86")}");
                        break;
                }
                process.Exited += OnProcessExited;
                process.Start();
            }
            catch (Exception ex)
            {
                process?.Dispose();
                process = null;
                Interface.uMod.LogException($"Exception while starting compiler version {CompilerVersion}: ", ex);
                if (BinaryPath.Contains("'"))
                {
                    Interface.uMod.LogWarning("Server directory path contains an apostrophe, compiler will not work until path is renamed");
                }
                else if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    Interface.uMod.LogWarning("Compiler may not be set as executable; chmod +x or 0744/0755 required");
                }

                if (ex.GetBaseException() != ex)
                {
                    Interface.uMod.LogException("BaseException: ", ex.GetBaseException());
                }

                Win32Exception win32 = ex as Win32Exception;
                if (win32 != null)
                {
                    Interface.uMod.LogError("Win32 NativeErrorCode: {0} ErrorCode: {1} HelpLink: {2}", win32.NativeErrorCode, win32.ErrorCode, win32.HelpLink);
                }
            }

            if (process == null)
            {
                return false;
            }

            client = new ObjectStreamClient<CompilerMessage>(process.StandardOutput.BaseStream, process.StandardInput.BaseStream);
            client.Message += OnMessage;
            client.Error += OnError;
            client.Start();
            return true;
        }

        private void OnProcessExited(object sender, EventArgs eventArgs)
        {
            Interface.uMod.NextTick(() =>
            {
                OnCompilerFailed($"compiler version {CompilerVersion} was closed unexpectedly");

                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    string envPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH");
                    string libraryPath = Path.Combine(Interface.uMod.ExtensionDirectory, IntPtr.Size == 8 ? "x64" : "x86");
                    if (string.IsNullOrEmpty(envPath) || !envPath.Contains(libraryPath))
                    {
                        Interface.uMod.LogWarning($"LD_LIBRARY_PATH does not container path to compiler dependencies: {libraryPath}");
                    }
                    else
                    {
                        Interface.uMod.LogWarning("User running server may not have the proper permissions or install is missing files");

                        Interface.uMod.LogWarning($"User running server: {Environment.UserName}");
                        UnixFileInfo compilerFileInfo = new UnixFileInfo(BinaryPath);
                        Interface.uMod.LogWarning($"Compiler under user/group: {compilerFileInfo.OwnerUser}/{compilerFileInfo.OwnerGroup}");

                        string depPath = Path.Combine(Interface.uMod.ExtensionDirectory, IntPtr.Size == 8 ? "x64" : "x86");
                        string[] depFiles = { "libmonoboehm-2.0.so.1", "libMonoPosixHelper.so", "mono-2.0.dll" };
                        foreach (string file in depFiles)
                        {
                            string filePath = Path.Combine(depPath, file);
                            if (!File.Exists(filePath))
                            {
                                Interface.uMod.LogWarning($"{filePath} is missing");
                            }
                        }
                    }
                }
                else
                {
                    string envPath = Environment.GetEnvironmentVariable("PATH");
                    string libraryPath = Path.Combine(Interface.uMod.ExtensionDirectory, "x86");
                    if (string.IsNullOrEmpty(envPath) || !envPath.Contains(libraryPath))
                    {
                        Interface.uMod.LogWarning($"PATH does not container path to compiler dependencies: {libraryPath}");
                    }
                    else
                    {
                        Interface.uMod.LogWarning("Compiler may have been closed by interference from security software or install is missing files");

                        string depPath = Path.Combine(Interface.uMod.ExtensionDirectory, "x86");
                        string[] depFiles = { "mono-2.0.dll", "msvcp140.dll", "msvcr120.dll" };
                        foreach (string file in depFiles)
                        {
                            string filePath = Path.Combine(depPath, file);
                            if (!File.Exists(filePath))
                            {
                                Interface.uMod.LogWarning($"{filePath} is missing");
                            }
                        }
                    }
                }

                Shutdown();
            });
        }

        private void OnCompilerFailed(string reason)
        {
            foreach (Compilation compilation in compilations.Values)
            {
                foreach (CompilablePlugin plugin in compilation.plugins)
                {
                    plugin.CompilerErrors = reason;
                }

                compilation.Completed();
            }
            compilations.Clear();
        }

        private static void PurgeOldLogs()
        {
            try
            {
                IEnumerable<string> filePaths = Directory.GetFiles(Interface.uMod.LogDirectory, "*.txt").Where(f =>
                {
                    string fileName = Path.GetFileName(f);
                    return fileName != null && fileName.StartsWith("compiler_");
                });
                foreach (string filePath in filePaths)
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception)
            {
                // Ignored
            }
        }

        private static string EscapePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "\"\"";
            }

            path = Regex.Replace(path, @"(\\*)" + "\"", @"$1\$0");
            path = Regex.Replace(path, @"^(.*\s.*?)(\\*)$", "\"$1$2$2\"");
            return path;
        }

        private static class Algorithms
        {
            public static readonly HashAlgorithm MD5 = new MD5CryptoServiceProvider();
            public static readonly HashAlgorithm SHA1 = new SHA1Managed();
            public static readonly HashAlgorithm SHA256 = new SHA256Managed();
            public static readonly HashAlgorithm SHA384 = new SHA384Managed();
            public static readonly HashAlgorithm SHA512 = new SHA512Managed();
            public static readonly HashAlgorithm RIPEMD160 = new RIPEMD160Managed();
        }

        private static string GetHash(string filePath, HashAlgorithm algorithm)
        {
            using (BufferedStream stream = new BufferedStream(File.OpenRead(filePath), 100000))
            {
                byte[] hash = algorithm.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
            }
        }
    }
}
