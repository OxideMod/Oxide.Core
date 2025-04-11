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
    /// Tests for circular reference detection in the Permission library.
    /// </summary>
    public class CircularReferenceTests : IDisposable
    {
        private readonly Permission permLib;
        private readonly string tempDataDir;
        private readonly FakePlugin testPlugin;
        private readonly string tempInstanceDir;
        private readonly string originalInstanceDir;

        public CircularReferenceTests()
        {
            // Setup the test environment
            var oxide = Interface.Oxide;
            var instanceDirProp = oxide.GetType().GetProperty("InstanceDirectory", BindingFlags.Public | BindingFlags.Instance);
            originalInstanceDir = instanceDirProp?.GetValue(oxide) as string;
            
            tempInstanceDir = Path.Combine(Path.GetTempPath(), "OxidePermissionTest", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempInstanceDir);
            
            if (instanceDirProp != null)
            {
                instanceDirProp.SetValue(oxide, tempInstanceDir);
            }
            
            tempDataDir = Path.Combine(tempInstanceDir, "data");
            Directory.CreateDirectory(tempDataDir);
            
            string tempLangDir = Path.Combine(tempInstanceDir, "lang");
            Directory.CreateDirectory(tempLangDir);
            
            string tempConfigDir = Path.Combine(tempInstanceDir, "config");
            Directory.CreateDirectory(tempConfigDir);
            
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
            
            Interface.Initialize();
            Interface.Oxide.Load();
            
            permLib = new Permission();
            testPlugin = new FakePlugin();
        }

        /// <summary>
        /// Cleans up the test environment by deleting temporary files.
        /// </summary>
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

        #region Circular Reference Tests

         /// <summary>
        /// Tests that HasCircularParent correctly identifies direct circular references between two groups.
        /// </summary>
        [Fact]
        public void HasCircularParent_DetectsDirectCircularReference()
        {
            permLib.CreateGroup("circA", "Circular A", 1);
            permLib.CreateGroup("circB", "Circular B", 1);
            
            permLib.SetGroupParent("circB", "circA");
            
            // Use reflection to access private method
            var method = typeof(Permission).GetMethod("HasCircularParent", BindingFlags.NonPublic | BindingFlags.Instance);
            bool hasCircular = (bool)method.Invoke(permLib, new object[] { "circA", "circB" });
            
            Assert.True(hasCircular);
        }

        /// <summary>
        /// Verifies that HasCircularParent detects indirect circular references through multiple levels of group hierarchy.
        /// </summary>
        [Fact]
        public void HasCircularParent_DetectsIndirectCircularReference()
        {
            permLib.CreateGroup("circX", "Circular X", 1);
            permLib.CreateGroup("circY", "Circular Y", 1);
            permLib.CreateGroup("circZ", "Circular Z", 1);
            
            permLib.SetGroupParent("circY", "circX");
            permLib.SetGroupParent("circZ", "circY");
            
            // Use reflection to access private method
            var method = typeof(Permission).GetMethod("HasCircularParent", BindingFlags.NonPublic | BindingFlags.Instance);
            bool hasCircular = (bool)method.Invoke(permLib, new object[] { "circX", "circZ" });
            
            Assert.True(hasCircular);
        }

        /// <summary>
        /// Tests that HasCircularParent returns false when there is no circular reference between groups.
        /// </summary>
        [Fact]
        public void HasCircularParent_ReturnsFalseForNonCircularReference()
        {
            permLib.CreateGroup("nonCircA", "Non-Circular A", 1);
            permLib.CreateGroup("nonCircB", "Non-Circular B", 1);
            
            // No parent set
            
            // Use reflection to access private method
            var method = typeof(Permission).GetMethod("HasCircularParent", BindingFlags.NonPublic | BindingFlags.Instance);
            bool hasCircular = (bool)method.Invoke(permLib, new object[] { "nonCircA", "nonCircB" });
            
            Assert.False(hasCircular);
        }

        #endregion
    }
} 