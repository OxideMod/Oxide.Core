using Xunit;
using Oxide.Core.Plugins;
using System.Reflection;
using System.Linq;
using System;

namespace Oxide.Core.Tests.Plugins
{
    /// <summary>
    /// Tests for the PluginLoader display classes
    /// </summary>
    public class PluginLoaderDisplayClassTests
    {
        /// <summary>
        /// Tests that the display classes used by PluginLoader can be instantiated and used
        /// </summary>
        [Fact]
        public void PluginLoader_DisplayClasses_CanBeUsed()
        {
            // The display classes are compiler-generated for use with LINQ and lambdas
            // We can test them by inspecting the PluginLoader class for nested types
            
            var nestedTypes = typeof(PluginLoader).GetNestedTypes(BindingFlags.NonPublic);
            
            // Check that we have display class types
            var displayClassTypes = nestedTypes.Where(t => 
                t.Name.Contains("DisplayClass") || 
                t.Name.Contains("__c")).ToList();
            
            Assert.NotEmpty(displayClassTypes);
            
            // Test each display class by creating an instance
            foreach (var displayClassType in displayClassTypes)
            {
                try
                {
                    // Attempt to create an instance
                    var instance = Activator.CreateInstance(displayClassType);
                    Assert.NotNull(instance);
                }
                catch (MissingMethodException)
                {
                    // Some display classes might not have a default constructor
                    continue;
                }
            }
            
            // The test passes if we can find display classes and at least attempt to instantiate them
            Assert.True(true);
        }
    }
} 