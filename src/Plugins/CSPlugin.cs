using Oxide.Core.Libraries;
using System;
using System.Collections.Generic;
using System.Reflection;

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

        // All matched hooked methods
        protected HookCache HooksCache = new HookCache();

        /// <summary>
        /// Initializes a new instance of the CSPlugin class
        /// </summary>
        public CSPlugin()
        {
            // Find all hooks in the plugin and any base classes derived from CSPlugin
            Type type = GetType();
            var types = new List<Type> { type };
            while (type != typeof(CSPlugin)) types.Add(type = type.BaseType);

            // Add hooks implemented in base classes before user implemented methods
            for (int i = types.Count - 1; i >= 0; i--)
            {
                foreach (MethodInfo method in types[i].GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                {
                    var attr = method.GetCustomAttributes(typeof(HookMethodAttribute), true);
                    if (attr.Length < 1) continue;

                    HookMethodAttribute hookmethod = attr[0] as HookMethodAttribute;
                    AddHookMethod(hookmethod?.Name, method);
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
            foreach (string hookname in Hooks.Keys) Subscribe(hookname);

            try
            {
                // Let the plugin know that it's loading
                OnCallHook("Init", null);
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogException($"Failed to initialize plugin '{Name} v{Version}'", ex);
                if (Loader != null) Loader.PluginErrors[Name] = ex.Message;
            }
        }

        protected void AddHookMethod(string name, MethodInfo method)
        {
            List<HookMethod> hookMethods;
            if (!Hooks.TryGetValue(name, out hookMethods))
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
            object returnvalue = null;
            bool pooledArray = false;

            // Call all hooks that match the signature
            foreach (HookMethod h in FindHooks(name, args))
            {
                int received = args?.Length ?? 0;
                object[] hookArgs;

                if (received != h.Parameters.Length)
                {
                    // The call argument count is different to the declared callback methods argument count
                    hookArgs = ArrayPool.Get(h.Parameters.Length);
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
                            ParameterInfo parameter = h.Parameters[n];
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
                    returnvalue = InvokeMethod(h, hookArgs);
                }
                catch (TargetInvocationException ex)
                {
                    if (pooledArray)
                    {
                        ArrayPool.Free(hookArgs);
                    }
                    throw ex.InnerException ?? ex;
                }

                if (received != h.Parameters.Length)
                {
                    // A copy of the call arguments was used for this method call
                    for (int n = 0; n < h.Parameters.Length; n++)
                    {
                        // Copy output values for out and by reference arguments back to the calling args
                        if (h.Parameters[n].IsOut || h.Parameters[n].ParameterType.IsByRef)
                        {
                            args[n] = hookArgs[n];
                        }
                    }
                }

                if (pooledArray)
                {
                    ArrayPool.Free(hookArgs);
                }
            }

            return returnvalue;
        }

        protected List<HookMethod> FindHooks(string name, object[] args)
        {
            // Get the full name of the hook `name(argument type 1, argument type 2, ..., argument type x)`

            // Check the cache if we already found a match for this hook
            HookCache cache;
            List<HookMethod> methods = HooksCache.GetHookMethod(name, args, out cache);
            if (methods != null)
            {
                return methods;
            }
            var matches = new List<HookMethod>();
            // Get all hook methods that could match, return an empty list if none match
            if (!Hooks.TryGetValue(name, out methods))
            {
                return matches;
            }

            // Find matching hooks
            HookMethod exactMatch = null;
            HookMethod overloadedMatch = null;

            foreach (HookMethod h in methods)
            {
                // A base hook should always have a matching signature either directly or through inheritance
                // and should always be called as core functionality depends on it.
                if (h.IsBaseHook)
                {
                    matches.Add(h);
                    continue;
                }

                // Check if this method matches the hook arguments passed if it isn't a base hook
                object[] hookArgs;
                int received = args?.Length ?? 0;

                bool pooledArray = false;

                if (received != h.Parameters.Length)
                {
                    // The call argument count is different to the declared callback methods argument count
                    hookArgs = ArrayPool.Get(h.Parameters.Length);
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
                            ParameterInfo parameter = h.Parameters[n];
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

                bool isExactMatch;
                if (h.HasMatchingSignature(hookArgs, out isExactMatch))
                {
                    if (isExactMatch)
                    {
                        exactMatch = h;
                        break;
                    }

                    // Should we determine the level and call the closest overloaded match? Performance impact?
                    overloadedMatch = h;
                }

                if (pooledArray)
                {
                    ArrayPool.Free(hookArgs);
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
