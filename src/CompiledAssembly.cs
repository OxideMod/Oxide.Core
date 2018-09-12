extern alias References;

using References::Mono.Cecil;
using References::Mono.Cecil.Cil;
using References::Mono.Cecil.Rocks;
using References::Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using MethodAttributes = References::Mono.Cecil.MethodAttributes;
using MethodBody = References::Mono.Cecil.Cil.MethodBody;

namespace uMod.Plugins
{
    public class CompiledAssembly
    {
        public CompilablePlugin[] CompilablePlugins;
        public string[] PluginNames;
        public string Name;
        public DateTime CompiledAt;
        public byte[] RawAssembly;
        public byte[] PatchedAssembly;
        public float Duration;
        public Assembly LoadedAssembly;
        public bool IsLoading;
        public bool IsBatch => CompilablePlugins.Length > 1;

        private List<Action<bool>> loadCallbacks = new List<Action<bool>>();
        private bool isPatching;
        private bool isLoaded;

        private static IEnumerable<string> BlacklistedNamespaces => new[] {
            "uMod.ServerConsole", "System.IO", "System.Net", "System.Xml", "System.Reflection.Assembly", "System.Reflection.Emit", "System.Threading",
            "System.Runtime.InteropServices", "System.Diagnostics", "System.Security", "System.Timers", "Mono.CSharp", "Mono.Cecil", "ServerFileSystem"
        };

        private static IEnumerable<string> WhitelistedNamespaces => new[] {
            "System.Diagnostics.Stopwatch", "System.IO.MemoryStream", "System.IO.Stream", "System.IO.BinaryReader", "System.IO.BinaryWriter", "System.Net.Dns",
            "System.Net.Dns.GetHostEntry", "System.Net.IPAddress", "System.Net.IPEndPoint", "System.Net.NetworkInformation",
            "System.Net.Sockets.SocketFlags", "System.Security.Cryptography", "System.Threading.Interlocked"
        };

        public CompiledAssembly(string name, CompilablePlugin[] plugins, byte[] rawAssembly, float duration)
        {
            Name = name;
            CompilablePlugins = plugins;
            RawAssembly = rawAssembly;
            Duration = duration;
            PluginNames = CompilablePlugins.Select(pl => pl.Name).ToArray();
        }

        public void LoadAssembly(Action<bool> callback)
        {
            if (isLoaded)
            {
                callback(true);
                return;
            }

            IsLoading = true;
            loadCallbacks.Add(callback);
            if (isPatching)
            {
                return;
            }

            PatchAssembly(rawAssembly =>
            {
                if (rawAssembly == null)
                {
                    foreach (Action<bool> cb in loadCallbacks)
                    {
                        cb(true);
                    }

                    loadCallbacks.Clear();
                    IsLoading = false;
                    return;
                }

                LoadedAssembly = Assembly.Load(rawAssembly);
                isLoaded = true;

                foreach (Action<bool> cb in loadCallbacks)
                {
                    cb(true);
                }

                loadCallbacks.Clear();

                IsLoading = false;
            });
        }

        private void PatchAssembly(Action<byte[]> callback)
        {
            if (isPatching)
            {
                Interface.uMod.LogWarning("Already patching plugin assembly: {0} (ignoring)", PluginNames.ToSentence());
                //RemoteLogger.Warning($"Already patching plugin assembly: {PluginNames.ToSentence()}");
                return;
            }

            float startedAt = Interface.uMod.Now;

            isPatching = true;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    AssemblyDefinition definition;
                    using (MemoryStream stream = new MemoryStream(RawAssembly))
                    {
                        definition = AssemblyDefinition.ReadAssembly(stream);
                    }

                    ConstructorInfo exceptionConstructor = typeof(UnauthorizedAccessException).GetConstructor(new[] { typeof(string) });
                    MethodReference securityException = definition.MainModule.Import(exceptionConstructor);

                    Action<TypeDefinition> patchModuleType = null;
                    patchModuleType = type =>
                    {
                        foreach (MethodDefinition method in type.Methods)
                        {
                            bool changedMethod = false;

                            if (method.Body == null)
                            {
                                if (method.HasPInvokeInfo)
                                {
                                    method.Attributes &= ~MethodAttributes.PInvokeImpl;
                                    MethodBody body = new MethodBody(method);
                                    body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "PInvoke access is restricted, you are not allowed to use PInvoke"));
                                    body.Instructions.Add(Instruction.Create(OpCodes.Newobj, securityException));
                                    body.Instructions.Add(Instruction.Create(OpCodes.Throw));
                                    method.Body = body;
                                }
                            }
                            else
                            {
                                bool replacedMethod = false;
                                foreach (VariableDefinition variable in method.Body.Variables)
                                {
                                    if (!IsNamespaceBlacklisted(variable.VariableType.FullName))
                                    {
                                        continue;
                                    }

                                    MethodBody body = new MethodBody(method);
                                    body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, $"System access is restricted, you are not allowed to use {variable.VariableType.FullName}"));
                                    body.Instructions.Add(Instruction.Create(OpCodes.Newobj, securityException));
                                    body.Instructions.Add(Instruction.Create(OpCodes.Throw));
                                    method.Body = body;
                                    replacedMethod = true;
                                    break;
                                }
                                if (replacedMethod)
                                {
                                    continue;
                                }

                                Collection<Instruction> instructions = method.Body.Instructions;
                                ILProcessor ilProcessor = method.Body.GetILProcessor();
                                Instruction first = instructions.First();

                                int i = 0;
                                while (i < instructions.Count)
                                {
                                    if (changedMethod)
                                    {
                                        break;
                                    }

                                    Instruction instruction = instructions[i];
                                    if (instruction.OpCode == OpCodes.Ldtoken)
                                    {
                                        IMetadataTokenProvider operand = instruction.Operand as IMetadataTokenProvider;
                                        string token = operand?.ToString();
                                        if (IsNamespaceBlacklisted(token))
                                        {
                                            ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Ldstr, $"System access is restricted, you are not allowed to use {token}"));
                                            ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Newobj, securityException));
                                            ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Throw));
                                            changedMethod = true;
                                        }
                                    }
                                    else if (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Calli || instruction.OpCode == OpCodes.Callvirt || instruction.OpCode == OpCodes.Ldftn)
                                    {
                                        MethodReference methodCall = instruction.Operand as MethodReference;
                                        string fullNamespace = methodCall?.DeclaringType.FullName;

                                        if (fullNamespace == "System.Type" && methodCall.Name == "GetType" || IsNamespaceBlacklisted(fullNamespace))
                                        {
                                            ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Ldstr, $"System access is restricted, you are not allowed to use {fullNamespace}"));
                                            ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Newobj, securityException));
                                            ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Throw));
                                            changedMethod = true;
                                        }
                                    }
                                    else if (instruction.OpCode == OpCodes.Ldfld)
                                    {
                                        FieldReference fieldType = instruction.Operand as FieldReference;
                                        string fullNamespace = fieldType?.FieldType.FullName;
                                        if (IsNamespaceBlacklisted(fullNamespace))
                                        {
                                            ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Ldstr, $"System access is restricted, you are not allowed to use {fullNamespace}"));
                                            ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Newobj, securityException));
                                            ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Throw));
                                            changedMethod = true;
                                        }
                                    }
                                    i++;
                                }
                            }

                            if (changedMethod)
                            {
                                method.Body?.OptimizeMacros();
                                /*//Interface.uMod.LogDebug("Updating {0} instruction offsets: {1}", instructions.Count, method.FullName);
                                int curoffset = 0;
                                for (int i = 0; i < instructions.Count; i++)
                                {
                                    var instruction = instructions[i];
                                    instruction.Previous = (i == 0) ? null : instructions[i - 1];
                                    instruction.Next = (i == instructions.Count - 1) ? null : instructions[i + 1];
                                    instruction.Offset = curoffset;
                                    curoffset += instruction.GetSize();
                                    //Interface.uMod.LogDebug("    {0}", instruction.ToString());
                                }*/
                            }
                        }
                        foreach (TypeDefinition nestedType in type.NestedTypes)
                        {
                            patchModuleType(nestedType);
                        }
                    };

                    foreach (TypeDefinition type in definition.MainModule.Types)
                    {
                        patchModuleType(type);

                        if (IsCompilerGenerated(type))
                        {
                            continue;
                        }

                        if (type.Namespace == "uMod.Plugins")
                        {
                            if (PluginNames.Contains(type.Name))
                            {
                                MethodDefinition constructor =
                                    type.Methods.FirstOrDefault(
                                        m => !m.IsStatic && m.IsConstructor && !m.HasParameters && !m.IsPublic);
                                if (constructor != null)
                                {
                                    CompilablePlugin plugin = CompilablePlugins.SingleOrDefault(p => p.Name == type.Name);
                                    if (plugin != null)
                                    {
                                        plugin.CompilerErrors = "Primary constructor in main class must be public";
                                    }
                                }
                                else
                                {
                                    new DirectCallMethod(definition.MainModule, type);
                                }
                            }
                            else
                            {
                                Interface.uMod.LogWarning(PluginNames.Length == 1
                                    ? $"{PluginNames[0]} has polluted the global namespace by defining {type.Name}"
                                    : $"A plugin has polluted the global namespace by defining {type.Name}");
                                //RemoteLogger.Info($"A plugin has polluted the global namespace by defining {type.Name}: {PluginNames.ToSentence()}");
                            }
                        }
                        else if (type.FullName != "<Module>")
                        {
                            if (!PluginNames.Any(plugin => type.FullName.StartsWith($"uMod.Plugins.{plugin}")))
                            {
                                Interface.uMod.LogWarning(PluginNames.Length == 1
                                    ? $"{PluginNames[0]} has polluted the global namespace by defining {type.FullName}"
                                    : $"A plugin has polluted the global namespace by defining {type.FullName}");
                            }
                        }
                    }

                    // TODO: Why is there no error on boot using this?
                    foreach (TypeDefinition type in definition.MainModule.Types)
                    {
                        if (type.Namespace != "uMod.Plugins" || !PluginNames.Contains(type.Name))
                        {
                            continue;
                        }

                        foreach (MethodDefinition m in type.Methods.Where(m => !m.IsStatic && !m.HasGenericParameters && !m.ReturnType.IsGenericParameter && !m.IsSetter && !m.IsGetter))
                        {
                            foreach (ParameterDefinition parameter in m.Parameters)
                            {
                                foreach (CustomAttribute attribute in parameter.CustomAttributes)
                                {
                                    //Interface.uMod.LogInfo($"{m.FullName} - {parameter.Name} - {attribute.Constructor.FullName}");
                                }
                            }
                        }
                    }

                    using (MemoryStream stream = new MemoryStream())
                    {
                        definition.Write(stream);
                        PatchedAssembly = stream.ToArray();
                    }

                    Interface.uMod.NextTick(() =>
                    {
                        isPatching = false;
                        //Interface.uMod.LogDebug("Patching {0} assembly took {1:0.00} ms", ScriptName, Interface.uMod.Now - startedAt);
                        callback(PatchedAssembly);
                    });
                }
                catch (Exception ex)
                {
                    Interface.uMod.NextTick(() =>
                    {
                        isPatching = false;
                        Interface.uMod.LogException($"Exception while patching: {PluginNames.ToSentence()}", ex);
                        //RemoteLogger.Exception($"Exception while patching: {PluginNames.ToSentence()}", ex);
                        callback(null);
                    });
                }
            });
        }

        public bool IsOutdated() => CompilablePlugins.Any(pl => pl.GetLastModificationTime() != CompiledAt);

        private bool IsCompilerGenerated(TypeDefinition type) => type.CustomAttributes.Any(attr => attr.Constructor.DeclaringType.ToString().Contains("CompilerGeneratedAttribute"));

        private static bool IsNamespaceBlacklisted(string fullNamespace)
        {
            foreach (string namespaceName in BlacklistedNamespaces)
            {
                if (!fullNamespace.StartsWith(namespaceName))
                {
                    continue;
                }

                if (WhitelistedNamespaces.Any(fullNamespace.StartsWith))
                {
                    continue;
                }

                return true;
            }
            return false;
        }
    }
}
