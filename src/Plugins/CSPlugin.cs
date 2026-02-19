using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using HarmonyLib;
using Oxide.Core.Libraries;
using Oxide.Pooling;

namespace Oxide.Core.Plugins
{
    /// <summary>
    /// Indicates that the specified method should be a handler for a hook
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class HookMethodAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the hook to... hook
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the HookMethod class
        /// </summary>
        /// <param name="name"></param>
        public HookMethodAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Indicates that the specified class should automatically apply it's harmony patches
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoPatchAttribute : Attribute
    {
        public AutoPatchAttribute()
        {
        }
    }

    /// <summary>
    /// Represents a plugin implemented in .NET
    /// </summary>
    public abstract class CSPlugin : Plugin
    {
        /// <summary>
        /// Gets the library by the specified type or name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T GetLibrary<T>(string name = null) where T : Library => Interface.Oxide.GetLibrary<T>(name);

        // All hooked methods
        protected Dictionary<string, List<HookMethod>> Hooks = new Dictionary<string, List<HookMethod>>();

        // Harmony
        private Harmony _harmonyInstance;
        protected string HarmonyId => $"com.oxidemod.{Name}";
        protected Harmony HarmonyInstance
        {
            get
            {
                if (_harmonyInstance == null)
                {
                    _harmonyInstance = new Harmony(HarmonyId);
                }

                return _harmonyInstance;
            }
        }

        // All matched hooked methods
        protected HookCache HooksCache = new HookCache();

        /// <summary>
        /// Pool of <see cref="object"/> array's
        /// </summary>
        protected IArrayPool<object> ObjectArrayPool { get; }

        /// <summary>
        /// Initializes a new instance of the CSPlugin class
        /// </summary>
        public CSPlugin()
        {
            ObjectArrayPool = ArrayPool<object>.Shared;

            // Find all hooks in the plugin and any base classes derived from CSPlugin
            Type type = GetType();
            List<Type> types = new()
            {
                type
            };

            while (type != typeof(CSPlugin))
            {
                types.Add(type = type.BaseType);
            }

            // Add hooks implemented in base classes before user implemented methods
            int typeCount = types.Count;
            for (int i = typeCount - 1; i >= 0; i--)
            {
                MethodInfo[] methods = types[i].GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                int methodCount = methods.Length;
                for (int j = 0; j < methodCount; j++)
                {
                    MethodInfo method = methods[j];
                    object[] customAttributes = method.GetCustomAttributes(typeof(HookMethodAttribute), true);
                    if (customAttributes.Length < 1)
                    {
                        continue;
                    }

                    HookMethodAttribute hookMethodAttribute = (HookMethodAttribute)customAttributes[0];
                    AddHookMethod(hookMethodAttribute?.Name, method);
                }
            }
        }

        /// <summary>
        /// Called when this plugin has been added to a manager
        /// </summary>
        /// <param name="manager"></param>
        public override void HandleAddedToManager(PluginManager manager)
        {
            // Let base work
            base.HandleAddedToManager(manager);

            // Subscribe us
            foreach (string hookName in Hooks.Keys)
            {
                Subscribe(hookName);
            }

            try
            {
                // Let the plugin know that it is loading
                OnCallHook("Init", null);
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogException($"Failed to initialize plugin '{Name} v{Version}'", ex);
                if (Loader != null)
                {
                    Loader.GetPluginErrors(Name).Add(ex.Message);
                }
            }

            // Find all classes with the AutoPatch attribute and apply the patches
            Type[] nestedTypes = GetType().GetNestedTypes(BindingFlags.DeclaredOnly | BindingFlags.Public |
                                                          BindingFlags.NonPublic | BindingFlags.Static);

            int nestedTypeCount = nestedTypes.Length;
            for (int i = 0; i < nestedTypeCount; i++)
            {
                Type nestedType = nestedTypes[i];
                object[] attr = nestedType.GetCustomAttributes(typeof(AutoPatchAttribute), false);
                if (attr.Length < 1)
                {
                    continue;
                }

                try
                {
                    List<MethodInfo>? harmonyMethods = HarmonyInstance.CreateClassProcessor(nestedType)?.Patch();
                    if (harmonyMethods == null || harmonyMethods.Count == 0)
                    {
                        Interface.Oxide.LogWarning(
                            $"[{Title}] AutoPatch attribute found on '{nestedType.Name}' but no HarmonyPatch methods found. Skipping.");
                        continue;
                    }

                    int harmonyMethodCount = harmonyMethods.Count;
                    for (int j = 0; j < harmonyMethodCount; j++)
                    {
                        MethodInfo harmonyMethod = harmonyMethods[j];
                        Interface.Oxide.LogInfo(
                            $"[{Title}] Automatically Harmony patched '{harmonyMethod?.Name ?? "unknown"}' method. ({nestedType.Name})");
                    }
                }
                catch (Exception ex)
                {
                    Interface.Oxide.LogException($"[{Title}] Failed to automatically Harmony patch '{nestedType.Name}'",
                        ex);
                }
            }
        }

        /// <summary>
        /// Called when this plugin has been removed from a manager
        /// </summary>
        /// <param name="manager"></param>
        public override void HandleRemovedFromManager(PluginManager manager)
        {
            // Unpatch all automatically patched Harmony patches
            _harmonyInstance?.UnpatchAll(HarmonyId);

            // Let base work
            base.HandleRemovedFromManager(manager);
        }

        protected void AddHookMethod(string name, MethodInfo method)
        {
            if (!Hooks.TryGetValue(name, out List<HookMethod> hookMethods))
            {
                hookMethods = new List<HookMethod>();
                Hooks[name] = hookMethods;
            }

            hookMethods.Add(new HookMethod(method));
        }

        /// <summary>
        /// Calls the specified hook on this plugin
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected sealed override object OnCallHook(string name, object[] args)
        {
            object returnValue = null;
            bool pooledArray = false;

            // Call all hooks that match the signature
            List<HookMethod> hookMethods = FindHooks(name, args);
            int hookMethodCount = hookMethods.Count;
            for (int i = 0; i < hookMethodCount; i++)
            {
                HookMethod hookMethod = hookMethods[i];
                int received = args?.Length ?? 0;
                object[] hookArgs;

                if (received != hookMethod.Parameters.Length)
                {
                    // The call argument count is different to the declared callback methods argument count
                    hookArgs = ObjectArrayPool.Take(hookMethod.Parameters.Length);
                    pooledArray = true;

                    if (received > 0 && hookArgs.Length > 0)
                    {
                        // Remove any additional arguments which the callback method does not declare
                        Array.Copy(args, hookArgs, Math.Min(received, hookArgs.Length));
                    }

                    if (hookArgs.Length > received)
                    {
                        // Create additional parameters for arguments excluded in this hook call
                        for (int n = received; n < hookArgs.Length; n++)
                        {
                            ParameterInfo parameter = hookMethod.Parameters[n];
                            if (parameter.DefaultValue != null && parameter.DefaultValue != DBNull.Value)
                            {
                                // Use the default value that was provided by the method definition
                                hookArgs[n] = parameter.DefaultValue;
                            }
                            else if (parameter.ParameterType.IsValueType)
                            {
                                // Use the default value for value types
                                hookArgs[n] = Activator.CreateInstance(parameter.ParameterType);
                            }
                        }
                    }
                }
                else
                {
                    hookArgs = args;
                }

                try
                {
                    returnValue = InvokeMethod(hookMethod, hookArgs);
                }
                catch (TargetInvocationException exception)
                {
                    if (pooledArray)
                    {
                        ObjectArrayPool.Return(hookArgs);
                    }

                    Exception? innerException = exception.InnerException;
                    if (innerException != null)
                    {
                        ExceptionDispatchInfo.Capture(innerException).Throw();
                    }

                    throw;
                }

                if (received != hookMethod.Parameters.Length)
                {
                    // A copy of the call arguments was used for this method call
                    for (int n = 0; n < hookMethod.Parameters.Length; n++)
                    {
                        // Copy output values for out and by reference arguments back to the calling args
                        if (hookMethod.Parameters[n].IsOut || hookMethod.Parameters[n].ParameterType.IsByRef)
                        {
                            args[n] = hookArgs[n];
                        }
                    }
                }

                if (pooledArray)
                {
                    ObjectArrayPool.Return(hookArgs);
                }
            }

            return returnValue;
        }

        protected List<HookMethod> FindHooks(string name, object[] args)
        {
            // Get the full name of the hook `name(argument type 1, argument type 2, ..., argument type x)`

            // Check the cache if we already found a match for this hook
            List<HookMethod> hookMethods = HooksCache.GetHookMethod(name, args, out HookCache cache);
            if (hookMethods != null)
            {
                return hookMethods;
            }

            List<HookMethod> matches = new List<HookMethod>();
            // Get all hook methods that could match, return an empty list if none match
            if (!Hooks.TryGetValue(name, out hookMethods))
            {
                return matches;
            }

            // Find matching hooks
            HookMethod exactMatch = null;
            HookMethod overloadedMatch = null;

            int hookMethodCount = hookMethods.Count;
            for (int i = 0; i < hookMethodCount; i++)
            {
                HookMethod hookMethod = hookMethods[i];
                // A base hook should always have a matching signature either directly or through inheritance
                // and should always be called as core functionality depends on it.
                if (hookMethod.IsBaseHook)
                {
                    matches.Add(hookMethod);
                    continue;
                }

                // Check if this method matches the hook arguments passed if it isn't a base hook
                object[] hookArgs;
                int received = args?.Length ?? 0;

                bool pooledArray = false;

                if (received != hookMethod.Parameters.Length)
                {
                    // The call argument count is different to the declared callback methods argument count
                    hookArgs = ObjectArrayPool.Take(hookMethod.Parameters.Length);
                    pooledArray = true;

                    if (received > 0 && hookArgs.Length > 0)
                    {
                        // Remove any additional arguments which the callback method does not declare
                        Array.Copy(args, hookArgs, Math.Min(received, hookArgs.Length));
                    }

                    if (hookArgs.Length > received)
                    {
                        // Create additional parameters for arguments excluded in this hook call
                        for (int n = received; n < hookArgs.Length; n++)
                        {
                            ParameterInfo parameter = hookMethod.Parameters[n];
                            if (parameter.DefaultValue != null && parameter.DefaultValue != DBNull.Value)
                            {
                                // Use the default value that was provided by the method definition
                                hookArgs[n] = parameter.DefaultValue;
                            }
                            else if (parameter.ParameterType.IsValueType)
                            {
                                // Use the default value for value types
                                hookArgs[n] = Activator.CreateInstance(parameter.ParameterType);
                            }
                        }
                    }
                }
                else
                {
                    hookArgs = args;
                }

                if (hookMethod.HasMatchingSignature(hookArgs, out bool isExactMatch))
                {
                    if (isExactMatch)
                    {
                        exactMatch = hookMethod;
                        break;
                    }

                    // Should we determine the level and call the closest overloaded match? Performance impact?
                    overloadedMatch = hookMethod;
                }

                if (pooledArray)
                {
                    ObjectArrayPool.Return(hookArgs);
                }
            }

            if (exactMatch != null)
            {
                matches.Add(exactMatch);
            }
            else
            {
                if (overloadedMatch != null)
                {
                    matches.Add(overloadedMatch);
                }
            }

            cache.SetupMethods(matches);

            return matches;
        }

        protected virtual object InvokeMethod(HookMethod method, object[] args) => method.Method.Invoke(this, args);
    }
}
