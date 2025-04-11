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
    /// Tests for group management in the Permission library.
    /// </summary>
    public class GroupTests : IDisposable
    {
        private readonly Permission permLib;
        private readonly string tempDataDir;
        private readonly FakePlugin testPlugin;
        private readonly string tempInstanceDir;

        public GroupTests()
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

        #region Group Tests

       /// <summary>
        /// Tests that CreateGroup successfully creates a new permission group with specified properties.
        /// </summary>
        [Fact]
        public void CreateGroup_CreatesGroupSuccessfully()
        {
            bool created = permLib.CreateGroup("newGroup", "New Group", 1);
            Assert.True(created);
            
            var groupData = permLib.GetGroupData("newGroup");
            Assert.NotNull(groupData);
            Assert.Equal("New Group", groupData.Title);
            Assert.Equal(1, groupData.Rank);
        }

        /// <summary>
        /// Verifies that CreateGroup fails when attempting to create a group with a name that already exists.
        /// </summary>
        [Fact]
        public void CreateGroup_FailsForExistingGroup()
        {
            permLib.CreateGroup("existingGroup", "Existing Group", 1);
            bool created = permLib.CreateGroup("existingGroup", "Duplicate Group", 2);
            Assert.False(created);
        }

        /// <summary>
        /// Tests that RemoveGroup successfully removes an existing permission group.
        /// </summary>
        [Fact]
        public void RemoveGroup_RemovesGroupSuccessfully()
        {
            permLib.CreateGroup("removeGroup", "Remove Group", 1);
            bool removed = permLib.RemoveGroup("removeGroup");
            Assert.True(removed);
            Assert.Null(permLib.GetGroupData("removeGroup"));
        }

        /// <summary>
        /// Ensures that RemoveGroup returns false when attempting to remove a non-existent group.
        /// </summary>
        [Fact]
        public void RemoveGroup_FailsForNonExistingGroup()
        {
            bool removed = permLib.RemoveGroup("nonExistingGroup");
            Assert.False(removed);
        }

        /// <summary>
        /// Verifies that RemoveGroup removes the group from all users who are members of the group.
        /// </summary>
        [Fact]
        public void RemoveGroup_RemovesFromUserData()
        {
            string groupName = "removeFromUsers";
            string userId = "userGroupRemove";
            
            permLib.CreateGroup(groupName, "Remove From Users", 1);
            permLib.AddUserGroup(userId, groupName);
            
            // Verify user has group before removal
            Assert.Contains(groupName, permLib.GetUserGroups(userId));
            
            // Remove group
            permLib.RemoveGroup(groupName);
            
            // Verify group is removed from user
            Assert.DoesNotContain(groupName, permLib.GetUserGroups(userId));
        }

        /// <summary>
        /// Tests that GetGroupData returns null when attempting to retrieve data for a non-existent group.
        /// </summary>
        [Fact]
        public void GetGroupData_ReturnsNullForNonExistingGroup()
        {
            Assert.Null(permLib.GetGroupData("nonexistentGroup"));
        }

        /// <summary>
        /// Verifies that GetGroups returns all created permission groups.
        /// </summary>
        [Fact]
        public void GetGroups_ReturnsCreatedGroups()
        {
            permLib.CreateGroup("groupTest1", "Group Test 1", 1);
            permLib.CreateGroup("groupTest2", "Group Test 2", 2);
            
            var groups = permLib.GetGroups();
            Assert.Contains("groupTest1", groups);
            Assert.Contains("groupTest2", groups);
        }

        /// <summary>
        /// Tests that GetUsersInGroup returns all users who are members of a specific group.
        /// </summary>
        [Fact]
        public void GetUsersInGroup_ReturnsCorrectUsers()
        {
            string groupName = "groupUsers";
            permLib.CreateGroup(groupName, "Group Users", 1);
            
            string user1 = "userInGroup1";
            string user2 = "userInGroup2";
            
            permLib.AddUserGroup(user1, groupName);
            permLib.AddUserGroup(user2, groupName);
            
            var users = permLib.GetUsersInGroup(groupName);
            Assert.Contains(users, s => s.Contains(user1));
            Assert.Contains(users, s => s.Contains(user2));
        }

        /// <summary>
        /// Verifies that GetGroupTitle returns the correct title for an existing group.
        /// </summary>
        [Fact]
        public void GetGroupTitle_ReturnsCorrectTitle()
        {
            permLib.CreateGroup("titleGroup", "Group Title Test", 1);
            string title = permLib.GetGroupTitle("titleGroup");
            Assert.Equal("Group Title Test", title);
        }

        /// <summary>
        /// Tests that GetGroupTitle returns an empty string for a non-existent group.
        /// </summary>
        [Fact]
        public void GetGroupTitle_ReturnsEmptyForNonExistingGroup()
        {
            Assert.Equal(string.Empty, permLib.GetGroupTitle("nonexistentGroup"));
        }

        /// <summary>
        /// Verifies that SetGroupTitle successfully updates the title of an existing group.
        /// </summary>
        [Fact]
        public void SetGroupTitle_UpdatesTitle()
        {
            permLib.CreateGroup("updateTitle", "Original Title", 1);
            permLib.SetGroupTitle("updateTitle", "Updated Title");
            
            string title = permLib.GetGroupTitle("updateTitle");
            Assert.Equal("Updated Title", title);
        }

        /// <summary>
        /// Tests that GetGroupRank returns the correct rank for an existing group.
        /// </summary>
        [Fact]
        public void GetGroupRank_ReturnsCorrectRank()
        {
            permLib.CreateGroup("rankGroup", "Rank Group", 5);
            int rank = permLib.GetGroupRank("rankGroup");
            Assert.Equal(5, rank);
        }

        /// <summary>
        /// Tests that GetGroupRank returns zero for a non-existent group.
        /// </summary>
        [Fact]
        public void GetGroupRank_ReturnsZeroForNonExistingGroup()
        {
            Assert.Equal(0, permLib.GetGroupRank("nonexistentGroup"));
        }

        /// <summary>
        /// Verifies that SetGroupRank successfully updates the rank of an existing group.
        /// </summary>
        [Fact]
        public void SetGroupRank_UpdatesRank()
        {
            permLib.CreateGroup("updateRank", "Update Rank", 1);
            permLib.SetGroupRank("updateRank", 10);
            
            int rank = permLib.GetGroupRank("updateRank");
            Assert.Equal(10, rank);
        }

        #endregion
    }
} 