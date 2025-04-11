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
    /// Tests for user/group management in the Permission library.
    /// </summary>
    public class UserGroupTests : IDisposable
    {
        private readonly Permission permLib;
        private readonly string tempDataDir;
        private readonly FakePlugin testPlugin;
        private readonly string tempInstanceDir;

        public UserGroupTests()
        {
            tempInstanceDir = Path.Combine(Path.GetTempPath(), "OxidePermissionTest", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempInstanceDir);
            
            tempDataDir = Path.Combine(tempInstanceDir, "data");
            Directory.CreateDirectory(tempDataDir);
            
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

        #region User Group Tests

        [Fact]
        public void UserHasAnyGroup_ReturnsFalseForNewUser()
        {
            Assert.False(permLib.UserHasAnyGroup("userNoGroup"));
        }

        [Fact]
        public void UserHasAnyGroup_ReturnsTrueForUserWithGroup()
        {
            string userId = "userWithGroup";
            permLib.CreateGroup("testGroup", "Test Group", 1);
            permLib.AddUserGroup(userId, "testGroup");
            Assert.True(permLib.UserHasAnyGroup(userId));
        }

        [Fact]
        public void GetUserGroups_ReturnsEmptyForNewUser()
        {
            var groups = permLib.GetUserGroups("userEmpty");
            Assert.Empty(groups);
        }

        [Fact]
        public void GetUserGroups_ReturnsGroupsForUser()
        {
            string userId = "userWithGroups";
            permLib.CreateGroup("group1", "Group 1", 1);
            permLib.CreateGroup("group2", "Group 2", 2);
            permLib.AddUserGroup(userId, "group1");
            permLib.AddUserGroup(userId, "group2");
            
            var groups = permLib.GetUserGroups(userId);
            Assert.Equal(2, groups.Length);
            Assert.Contains("group1", groups);
            Assert.Contains("group2", groups);
        }

        [Fact]
        public void AddUserGroup_AddGroupSuccessfully()
        {
            string userId = "userAddGroup";
            permLib.CreateGroup("addGroup", "Add Group", 1);
            permLib.AddUserGroup(userId, "addGroup");
            Assert.Contains("addGroup", permLib.GetUserGroups(userId));
        }

        [Fact]
        public void AddUserGroup_DoesNothingForNonExistingGroup()
        {
            string userId = "userNoGroup";
            permLib.AddUserGroup(userId, "nonexistentGroup");
            var groups = permLib.GetUserGroups(userId);
            Assert.Empty(groups);
        }

        [Fact]
        public void RemoveUserGroup_WithWildcard_ClearsAllUserGroups()
        {
            bool created = permLib.CreateGroup("groupX", "Group X", 1);
            Assert.True(created);
            string userId = "userWildcardGroup";
            permLib.AddUserGroup(userId, "groupX");
            Assert.Contains("groupX", permLib.GetUserGroups(userId));
            permLib.RemoveUserGroup(userId, "*");
            Assert.Empty(permLib.GetUserGroups(userId));
        }

        [Fact]
        public void RemoveUserGroup_RemovesSpecificGroup()
        {
            string userId = "userRemoveGroup";
            permLib.CreateGroup("group1", "Group 1", 1);
            permLib.CreateGroup("group2", "Group 2", 2);
            permLib.AddUserGroup(userId, "group1");
            permLib.AddUserGroup(userId, "group2");
            
            permLib.RemoveUserGroup(userId, "group1");
            var groups = permLib.GetUserGroups(userId);
            Assert.Single(groups);
            Assert.Contains("group2", groups);
        }

        [Fact]
        public void UserHasGroup_ReturnsFalseForNonExistingGroup()
        {
            Assert.False(permLib.UserHasGroup("someUser", "nonexistentGroup"));
        }

        [Fact]
        public void UserHasGroup_ReturnsTrueForUserWithGroup()
        {
            string userId = "userHasGroup";
            permLib.CreateGroup("groupHas", "Group Has", 1);
            permLib.AddUserGroup(userId, "groupHas");
            Assert.True(permLib.UserHasGroup(userId, "groupHas"));
        }

        #endregion
    }
} 