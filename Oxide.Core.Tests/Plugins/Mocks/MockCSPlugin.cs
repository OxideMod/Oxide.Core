using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace Oxide.Core.Tests.Plugins.Mocks
{
    /// <summary>
    /// Mock CSPlugin implementation for testing
    /// </summary>
    public class MockCSPlugin : Plugin
    {
        // Create fields that would normally be initialized in CSPlugin
        private Dictionary<string, List<HookMethod>> Hooks = new Dictionary<string, List<HookMethod>>();
        private HookCache HooksCache = new HookCache();

        public MockCSPlugin()
        {
            // Set properties directly instead of relying on base constructor
            Name = "MockCSPlugin";
            Title = "Mock CS Plugin";
            Author = "Test";
            Version = new VersionNumber(1, 0, 0);
            
            // Add a test hook method
            var hookMethod = new HookMethod(GetType().GetMethod(nameof(TestHookHandler), 
                BindingFlags.NonPublic | BindingFlags.Instance));
            
            if (!Hooks.TryGetValue("TestHook", out var hookMethods))
            {
                hookMethods = new List<HookMethod>();
                Hooks["TestHook"] = hookMethods;
            }
            hookMethods.Add(hookMethod);
        }

        // Define a method with HookMethodAttribute for testing
        [HookMethod("TestHook")]
        private object TestHookHandler(object arg)
        {
            return $"Hook: TestHook, Args: 1";
        }
        
        public override void Load()
        {
            // No custom logic
        }
        
        // Implement OnCallHook for testing
        protected override object OnCallHook(string hook, object[] args)
        {
            if (hook == "TestHook" && args != null && args.Length > 0)
            {
                return $"Hook: {hook}, Args: {args.Length}";
            }
            return null;
        }
        
        // Add methods that would be in CSPlugin
        public string GetHarmonyId() => "com.oxidemod." + Name;
        
        public List<HookMethod> FindHooksPublic(string name, object[] args)
        {
            return new List<HookMethod> { new HookMethod(GetType().GetMethod(nameof(TestHookHandler), 
                BindingFlags.NonPublic | BindingFlags.Instance)) };
        }
    }
    
    /// <summary>
    /// Attribute to mark hook methods for testing
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class HookMethodAttribute : Attribute
    {
        public string Name { get; }
        
        public HookMethodAttribute(string name)
        {
            Name = name;
        }
    }
} 