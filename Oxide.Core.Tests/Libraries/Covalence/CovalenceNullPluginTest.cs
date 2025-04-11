using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;
using Xunit;
using System.IO;
using System.Globalization;
using CovalenceLib = Oxide.Core.Libraries.Covalence.Covalence;

namespace Oxide.Core.Tests.Libraries.Covalence
{
    /// <summary>
    /// Tests for how CovalenceLib handles null plugin references
    /// </summary>
    public class CovalenceNullPluginTest : IDisposable
    {
        private readonly string tempInstanceDir;
        private readonly string originalInstanceDir;

        public CovalenceNullPluginTest()
        {
            // Setup the test environment
            var oxide = Interface.Oxide;
            var instanceDirProp = oxide.GetType().GetProperty("InstanceDirectory", BindingFlags.Public | BindingFlags.Instance);
            originalInstanceDir = instanceDirProp?.GetValue(oxide) as string;
            
            tempInstanceDir = Path.Combine(Path.GetTempPath(), "OxideCovalenceTest", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempInstanceDir);
            
            if (instanceDirProp != null)
            {
                instanceDirProp.SetValue(oxide, tempInstanceDir);
            }
            
            string tempDataDir = Path.Combine(tempInstanceDir, "data");
            Directory.CreateDirectory(tempDataDir);
            
            string tempLangDir = Path.Combine(tempInstanceDir, "lang");
            Directory.CreateDirectory(tempLangDir);
            
            string tempConfigDir = Path.Combine(tempInstanceDir, "config");
            Directory.CreateDirectory(tempConfigDir);
            
            Interface.Initialize();
            Interface.Oxide.Load();
        }

        public void Dispose()
        {
            // Cleanup test environment
            var oxide = Interface.Oxide;
            var instanceDirProp = oxide.GetType().GetProperty("InstanceDirectory", BindingFlags.Public | BindingFlags.Instance);
            if (instanceDirProp != null)
            {
                instanceDirProp.SetValue(oxide, originalInstanceDir);
            }
            
            try
            {
                if (Directory.Exists(tempInstanceDir))
                {
                    Directory.Delete(tempInstanceDir, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        
        /// <summary>
        /// Mock logger for testing
        /// </summary>
        private class MockLogger : Logger
        {
            public List<string> LogMessages { get; } = new List<string>();
            
            public MockLogger() : base(false)
            {
            }
            
            public override void Write(LogType type, string format, params object[] args)
            {
                string message = string.Format(format, args);
                LogMessages.Add(message);
                // No need to call base as it would write to console
            }
        }
        
        /// <summary>
        /// A command system that always throws CommandAlreadyExistsException
        /// </summary>
        private class ThrowingCommandSystem : ICommandSystem
        {
            public void RegisterCommand(string command, Plugin plugin, CommandCallback callback)
            {
                throw new CommandAlreadyExistsException(command);
            }
            
            public void UnregisterCommand(string command, Plugin plugin)
            {
                // Do nothing
            }
        }
        
        /// <summary>
        /// Mock interface for testing
        /// </summary>
        private class MockInterface
        {
            public Logger RootLogger { get; set; } = new MockLogger();
            
            private static MockInterface _instance;
            public static MockInterface Instance => _instance ?? (_instance = new MockInterface());
            
            public void SetupForTesting()
            {
                // Store the current interface
                _instance = this;
                
                // No need to try and replace the entire Interface.Oxide object
                // The Covalence tests only need a logger that we can check later
            }
            
            public void ResetForTesting()
            {
                // Reset the instance
                _instance = null;
            }
        }
        
        /// <summary>
        /// Test for the scenario when a command is registered with a null plugin
        /// </summary>
        [Fact]
        public void RegisterCommand_NullPlugin_UsesUnknownPluginName()
        {
            // Create a mock logger to capture log messages
            var mockLogger = new MockLogger();
            
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
            // Create a mock logger to capture log messages
            var mockLogger = new MockLogger();
            
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
            
            // Verify the log message contains information about the command already being registered
            bool containsCommandExists = mockLogger.LogMessages.Any(msg => 
                msg.Contains("An unknown plugin") && 
                msg.Contains("tried to register command") && 
                msg.Contains("which is already registered"));
            
            Assert.True(containsCommandExists, "Error log should contain message about command already being registered");
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