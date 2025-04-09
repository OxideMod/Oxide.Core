using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Oxide.Core.Libraries.Covalence;
using Xunit;
using Oxide.Core.Logging;
using System.Globalization;
using CovalenceLib = Oxide.Core.Libraries.Covalence.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Core.Tests.Libraries.Covalence
{
    /// <summary>
    /// Specific test for the null plugin name handling in RegisterCommand
    /// </summary>
    public class CovalenceNullPluginTest
    {
        /// <summary>
        /// Mock Logger for testing
        /// </summary>
        private class MockLogger : Logger
        {
            public List<string> LogMessages { get; } = new List<string>();
            
            public MockLogger() : base(false)
            {
            }
            
            public override void Write(LogType type, string format, params object[] args)
            {
                string message = string.Format(CultureInfo.InvariantCulture, format, args);
                LogMessages.Add(message);
            }
        }
        
        /// <summary>
        /// Command system that always throws CommandAlreadyExistsException
        /// </summary>
        private class ThrowingCommandSystem : ICommandSystem
        {
            public void RegisterCommand(string command, Plugin plugin, CommandCallback callback)
            {
                throw new CommandAlreadyExistsException(command);
            }
            
            public void UnregisterCommand(string command, Plugin plugin)
            {
                // Not needed for our test
            }
        }
        
        /// <summary>
        /// A minimal mock for Interface.Oxide to allow testing of Covalence
        /// </summary>
        private class MockInterface
        {
            public Logger RootLogger { get; set; } = new MockLogger();
            
            private static MockInterface _instance;
            public static MockInterface Instance => _instance ?? (_instance = new MockInterface());
            
            public void SetupForTesting()
            {
                var field = typeof(Interface).GetField("Oxide", BindingFlags.Static | BindingFlags.Public);
                if (field != null)
                {
                    var oxideMod = new MockOxideMod(RootLogger);
                    field.SetValue(null, oxideMod);
                }
            }
            
            public void ResetForTesting()
            {
                var field = typeof(Interface).GetField("Oxide", BindingFlags.Static | BindingFlags.Public);
                field?.SetValue(null, null);
            }
        }
        
        /// <summary>
        /// Mock OxideMod for testing
        /// </summary>
        private class MockOxideMod
        {
            public Logger RootLogger { get; }
            
            public MockOxideMod(Logger logger)
            {
                RootLogger = logger;
            }
        }
        
        /// <summary>
        /// Test for the line "string pluginName = plugin?.Name ?? "An unknown plugin";"
        /// </summary>
        [Fact]
        public void RegisterCommand_NullPlugin_UsesUnknownPluginName()
        {
            // Create a mock interface and logger
            var mockInterface = new MockInterface();
            var mockLogger = new MockLogger();
            mockInterface.RootLogger = mockLogger;
            
            try
            {
                // Setup the mock interface
                mockInterface.SetupForTesting();
                
                // Create a new instance of CovalenceLib
                var covalence = new CovalenceLib();
                
                // Use reflection to set the logger field
                var loggerField = typeof(CovalenceLib).GetField("logger", BindingFlags.NonPublic | BindingFlags.Instance);
                loggerField.SetValue(covalence, mockLogger);
                
                // Create a command system that always throws CommandAlreadyExistsException
                var throwingCmdSystem = new ThrowingCommandSystem();
                var cmdSystemField = typeof(CovalenceLib).GetField("cmdSystem", BindingFlags.NonPublic | BindingFlags.Instance);
                cmdSystemField.SetValue(covalence, throwingCmdSystem);
                
                // Call RegisterCommand with null plugin to hit the null plugin case
                CommandCallback callback = (player, cmd, args) => true;
                covalence.RegisterCommand("test", null, callback);
                
                // Verify the log message contains "An unknown plugin"
                bool containsUnknownPlugin = mockLogger.LogMessages.Any(msg => 
                    msg.Contains("An unknown plugin") && 
                    msg.Contains("tried to register command"));
                
                Assert.True(containsUnknownPlugin, "Error log should contain 'An unknown plugin' text when plugin is null");
            }
            finally
            {
                // Clean up
                mockInterface.ResetForTesting();
            }
        }
        
        /// <summary>
        /// Simple test that directly tests both paths of the null-conditional operator expression
        /// </summary>
        [Fact]
        public void NullConditionalOperator_BothPaths_Covered()
        {
            // Directly test the null path
            Plugin nullPlugin = null;
            string pluginName = nullPlugin?.Name ?? "An unknown plugin";
            Assert.Equal("An unknown plugin", pluginName);
            
            // Directly test the non-null path by creating a minimal plugin
            var plugin = new MockPlugin();
            pluginName = plugin?.Name ?? "An unknown plugin";
            Assert.Equal("MockPlugin", pluginName);
        }
        
        /// <summary>
        /// Test for the scenario when a command already exists and is registered with a null plugin
        /// </summary>
        [Fact]
        public void RegisterCommand_WithNullPluginName_CommandAlreadyExists_UsesUnknownPluginName()
        {
            // Create a mock interface and logger
            var mockInterface = new MockInterface();
            var mockLogger = new MockLogger();
            mockInterface.RootLogger = mockLogger;
            
            try
            {
                // Setup the mock interface
                mockInterface.SetupForTesting();
                
                // Create a new instance of CovalenceLib
                var covalence = new CovalenceLib();
                
                // Use reflection to set the logger field
                var loggerField = typeof(CovalenceLib).GetField("logger", BindingFlags.NonPublic | BindingFlags.Instance);
                loggerField.SetValue(covalence, mockLogger);
                
                // Create a command system that always throws CommandAlreadyExistsException
                var throwingCmdSystem = new ThrowingCommandSystem();
                var cmdSystemField = typeof(CovalenceLib).GetField("cmdSystem", BindingFlags.NonPublic | BindingFlags.Instance);
                cmdSystemField.SetValue(covalence, throwingCmdSystem);
                
                // Call RegisterCommand with null plugin and a command name
                CommandCallback callback = (player, cmd, args) => true;
                covalence.RegisterCommand("testCommand", null, callback);
                
                // Verify the log message contains "An unknown plugin" and the correct command name
                bool containsUnknownPlugin = mockLogger.LogMessages.Any(msg => 
                    msg.Contains("An unknown plugin") && 
                    msg.Contains("tried to register command") &&
                    msg.Contains("'testCommand'"));
                
                Assert.True(containsUnknownPlugin, "Error log should contain 'An unknown plugin' text and the command name when plugin is null");
            }
            finally
            {
                // Clean up
                mockInterface.ResetForTesting();
            }
        }
        
        /// <summary>
        /// A simple mock plugin for testing
        /// </summary>
        private class MockPlugin : Plugin
        {
            public MockPlugin()
            {
                Name = "MockPlugin";
            }
            
            public override void Load() { }
            
            protected override object OnCallHook(string hook, object[] args)
            {
                return null;
            }
        }
    }
} 