using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Xunit;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Core.Tests.Plugins.Mocks;
using Moq;
using Oxide.Core.Logging;

namespace Oxide.Core.Tests.Libraries.PermissionTestModules
{
    /// <summary>
    /// Tests for plugin unload handling in the Permission library.
    /// </summary>
    public class PluginUnloadTests : IDisposable
    {
        private readonly Permission permLib;
        private readonly string tempDataDir;
        private readonly FakePlugin testPlugin;
        private readonly string tempInstanceDir;

        public PluginUnloadTests()
        {
            // Setup the test environment
            var oxide = Interface.Oxide;
            var instanceDirProp = oxide.GetType().GetProperty("InstanceDirectory", BindingFlags.Public | BindingFlags.Instance);
            string originalDir = instanceDirProp?.GetValue(oxide) as string;
            
            tempInstanceDir = Path.Combine(Path.GetTempPath(), "OxidePermissionTest", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempInstanceDir);
            
            // Set the temp directory as the instance directory
            if (instanceDirProp != null)
            {
                instanceDirProp.SetValue(oxide, tempInstanceDir);
            }
            
            // Create necessary directories
            tempDataDir = Path.Combine(tempInstanceDir, "data");
            Directory.CreateDirectory(tempDataDir);
            
            string tempLangDir = Path.Combine(tempInstanceDir, "lang");
            Directory.CreateDirectory(tempLangDir);
            
            string tempConfigDir = Path.Combine(tempInstanceDir, "config");
            Directory.CreateDirectory(tempConfigDir);
            
            // Create empty files to avoid NullReferenceException
            string usersFile = Path.Combine(tempDataDir, "oxide.users");
            if (!File.Exists(usersFile))
            {
                File.WriteAllText(usersFile, "{}");
            }
            
            string groupsFile = Path.Combine(tempDataDir, "oxide.groups");
            if (!File.Exists(groupsFile))
            {
                File.WriteAllText(groupsFile, "{}");
            }
            
            // Initialize and load Oxide
            Interface.Initialize();
            Interface.Oxide.Load();
            
            // Create permission library and test plugin
            permLib = new Permission();
            testPlugin = new FakePlugin();
        }

        /// <summary>
        /// Cleans up the test environment by deleting temporary files.
        /// </summary>
        public void Dispose()
        {
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

        #region Plugin Unload Tests

/// <summary>
        /// Verifies that when a plugin is removed from the plugin manager, all its registered permissions are also removed.
        /// </summary>
        [Fact]
        public void Owner_OnRemovedFromManager_RemovesPermissions()
        {
            string perm1 = $"{testPlugin.Name}.unreg1";
            string perm2 = $"{testPlugin.Name}.unreg2";
            
            permLib.RegisterPermission(perm1, testPlugin);
            permLib.RegisterPermission(perm2, testPlugin);
            
            // Verify permissions are registered
            var permissions = permLib.GetPermissions();
            Assert.Contains(perm1, permissions);
            Assert.Contains(perm2, permissions);
            
            // Call the private method that handles plugin removal
            var method = typeof(Permission).GetMethod("owner_OnRemovedFromManager", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(permLib, new object[] { testPlugin, null });
            
            // Verify permissions are unregistered
            permissions = permLib.GetPermissions();
            Assert.DoesNotContain(perm1, permissions);
            Assert.DoesNotContain(perm2, permissions);
        }

        #endregion
    }
} 