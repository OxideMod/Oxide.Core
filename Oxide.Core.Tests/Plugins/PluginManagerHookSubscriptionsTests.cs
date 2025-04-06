using Xunit;
using Oxide.Core.Plugins;
using System.Reflection;
using System.Collections.Generic;
using System;

namespace Oxide.Core.Tests.Plugins
{
    /// <summary>
    /// Tests for the PluginManager.HookSubscriptions class
    /// </summary>
    public class PluginManagerHookSubscriptionsTests
    {
        /// <summary>
        /// Tests that the HookSubscriptions class can be instantiated
        /// </summary>
        [Fact]
        public void HookSubscriptions_Constructor_InitializesProperties()
        {
            // Get the HookSubscriptions class as it's a private nested class
            Type hookSubscriptionsType = typeof(PluginManager).GetNestedType("HookSubscriptions", 
                BindingFlags.NonPublic);
            
            // Create an instance
            var hookSubscriptions = Activator.CreateInstance(hookSubscriptionsType);
            
            // Get properties
            var pluginsProperty = hookSubscriptionsType.GetProperty("Plugins");
            var callDepthProperty = hookSubscriptionsType.GetProperty("CallDepth");
            var pendingChangesProperty = hookSubscriptionsType.GetProperty("PendingChanges");
            
            // Assert properties are initialized
            Assert.NotNull(pluginsProperty.GetValue(hookSubscriptions));
            Assert.Equal(0, callDepthProperty.GetValue(hookSubscriptions));
            Assert.NotNull(pendingChangesProperty.GetValue(hookSubscriptions));
        }
    }
} 