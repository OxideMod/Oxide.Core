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
    /// Tests for group parent relationship management in the Permission library.
    /// </summary>
    public class GroupParentTests : IDisposable
    {
        private readonly Permission permLib;
        private readonly string tempDataDir;
        private readonly FakePlugin testPlugin;
        private readonly string tempInstanceDir;

        public GroupParentTests()
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

        #region Group Parent Tests

        /// <summary>
        /// Tests that GetGroupParent returns the correct parent group name or empty string if none is set.
        /// </summary>
        [Fact]
        public void GetGroupParent_ReturnsProperValue()
        {
            permLib.CreateGroup("parentGroup", "Parent", 1);
            permLib.CreateGroup("childGroup", "Child", 1);
            permLib.SetGroupParent("childGroup", "parentGroup");
            
            Assert.Equal("parentGroup", permLib.GetGroupParent("childGroup"));
            Assert.Equal(string.Empty, permLib.GetGroupParent("nonexistent"));
        }

        /// <summary>
        /// Verifies that SetGroupParent correctly establishes a parent-child relationship between groups.
        /// </summary>
        [Fact]
        public void SetGroupParent_SetsParentCorrectly()
        {
            permLib.CreateGroup("groupParent", "Parent", 1);
            permLib.CreateGroup("groupChild", "Child", 1);
            
            bool result = permLib.SetGroupParent("groupChild", "groupParent");
            Assert.True(result);
            Assert.Equal("groupParent", permLib.GetGroupParent("groupChild"));
        }

        /// <summary>
        /// Tests that SetGroupParent with an empty string clears the parent group relationship.
        /// </summary>
        [Fact]
        public void SetGroupParent_ClearsParentWithEmptyString()
        {
            permLib.CreateGroup("parentClear", "Parent Clear", 1);
            permLib.CreateGroup("childClear", "Child Clear", 1);
            
            permLib.SetGroupParent("childClear", "parentClear");
            Assert.Equal("parentClear", permLib.GetGroupParent("childClear"));
            
            permLib.SetGroupParent("childClear", string.Empty);
            
            // Verify internally that the parent is null after clearing
            // This directly accesses the groupsData field to check the actual stored value
            var groupsDataField = typeof(Permission).GetField("groupsData", BindingFlags.NonPublic | BindingFlags.Instance);
            var groupsData = groupsDataField.GetValue(permLib) as Dictionary<string, GroupData>;
            var childData = groupsData["childClear"];
            
            // Assert that the internal ParentGroup property is set to null or empty string
            // Either is acceptable as the GetGroupParent method normalizes null to empty string
            Assert.True(string.IsNullOrEmpty(childData.ParentGroup));
        }

        /// <summary>
        /// Verifies that SetGroupParent prevents direct circular parent references between groups.
        /// </summary>
        [Fact]
        public void SetGroupParent_PreventsCircularReference()
        {
            permLib.CreateGroup("groupA", "Group A", 1);
            permLib.CreateGroup("groupB", "Group B", 1);
            
            bool setParentB = permLib.SetGroupParent("groupB", "groupA");
            Assert.True(setParentB);
            
            bool setParentA = permLib.SetGroupParent("groupA", "groupB");
            Assert.False(setParentA);
        }

        /// <summary>
        /// Tests that SetGroupParent prevents deep indirect circular parent references across multiple groups.
        /// </summary>
        [Fact]
        public void SetGroupParent_PreventsDeepCircularReference()
        {
            permLib.CreateGroup("groupX", "Group X", 1);
            permLib.CreateGroup("groupY", "Group Y", 1);
            permLib.CreateGroup("groupZ", "Group Z", 1);
            
            permLib.SetGroupParent("groupY", "groupX");
            permLib.SetGroupParent("groupZ", "groupY");
            
            bool setParentX = permLib.SetGroupParent("groupX", "groupZ");
            Assert.False(setParentX);
        }

        #endregion
    }
} 