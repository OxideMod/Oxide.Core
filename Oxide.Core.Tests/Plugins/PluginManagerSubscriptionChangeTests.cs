using Xunit;
using Oxide.Core.Plugins;
using System.Reflection;
using Oxide.Core.Tests.Plugins.Mocks;
using System;

namespace Oxide.Core.Tests.Plugins
{
    /// <summary>
    /// Tests for the PluginManager.SubscriptionChange struct
    /// </summary>
    public class PluginManagerSubscriptionChangeTests
    {
        /// <summary>
        /// Tests that SubscriptionChange constructor sets properties correctly
        /// </summary>
        [Fact]
        public void SubscriptionChange_Constructor_SetsProperties()
        {
            // Get the nested types using reflection
            Type subscriptionChangeType = typeof(PluginManager).GetNestedType("SubscriptionChange", BindingFlags.NonPublic);
            Type changeTypeEnum = typeof(PluginManager).GetNestedType("SubscriptionChangeType", BindingFlags.NonPublic);
            
            Assert.NotNull(subscriptionChangeType);
            Assert.NotNull(changeTypeEnum);
            
            // Get the constructor
            ConstructorInfo constructor = subscriptionChangeType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(Plugin), changeTypeEnum },
                null);
            
            Assert.NotNull(constructor);
            
            // Get the enum value for Subscribe
            object subscribeValue = Enum.GetValues(changeTypeEnum).GetValue(0); // 0 = Subscribe
            
            // Create a plugin instance
            var plugin = new FakePlugin();
            
            // Create an instance of SubscriptionChange
            object subscriptionChange = constructor.Invoke(new[] { plugin, subscribeValue });
            
            // Get properties
            PropertyInfo pluginProperty = subscriptionChangeType.GetProperty("Plugin");
            PropertyInfo changeProperty = subscriptionChangeType.GetProperty("Change");
            
            // Assert
            Assert.NotNull(pluginProperty);
            Assert.NotNull(changeProperty);
            
            Assert.Same(plugin, pluginProperty.GetValue(subscriptionChange));
            Assert.Equal(subscribeValue, changeProperty.GetValue(subscriptionChange));
        }
    }
} 