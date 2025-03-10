using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Core.Tests.Plugins.Mocks; 

namespace Oxide.Core.Tests.Libraries
{
    public class PermissionTest : IDisposable
    {
        private readonly Permission permLib;
        private readonly string tempInstanceDir;
        private readonly string tempDataDir;
        private readonly string originalInstanceDir;

        public PermissionTest()
        {
            var oxide = Interface.Oxide;
            var instanceDirProp = oxide.GetType().GetProperty("InstanceDirectory", BindingFlags.Public | BindingFlags.Instance);
            originalInstanceDir = instanceDirProp?.GetValue(oxide) as string;

            // Create a unique temporary folder for testing.
            tempInstanceDir = Path.Combine(Path.GetTempPath(), "OxidePermissionTest", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempInstanceDir);

            // Set InstanceDirectory to our temporary folder.
            if (instanceDirProp != null)
            {
                instanceDirProp.SetValue(oxide, tempInstanceDir);
            }

            // Create required subdirectories.
            tempDataDir = Path.Combine(tempInstanceDir, "data");
            Directory.CreateDirectory(tempDataDir);
            string tempLangDir = Path.Combine(tempInstanceDir, "lang");
            Directory.CreateDirectory(tempLangDir);
            string tempConfigDir = Path.Combine(tempInstanceDir, "config");
            Directory.CreateDirectory(tempConfigDir);

            // Create empty data files for users and groups if they do not exist.
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

            // Reinitialize Oxide and load core components.
            Interface.Initialize();
            Interface.Oxide.Load();

            // Instantiate a new Permission library.
            permLib = new Permission();
        }

        #region Basic Query and Data Access Tests

        [Fact]
        public void GetUserData_ReturnsNewUserDataIfNotExist()
        {
            var userData = permLib.GetUserData("newUser");
            Assert.NotNull(userData);
            Assert.Equal("Unnamed", userData.LastSeenNickname);
        }

        [Fact]
        public void UpdateNickname_UpdatesExistingUserData()
        {
            var userData = permLib.GetUserData("userNick");
            userData.LastSeenNickname = "OldName";
            permLib.UpdateNickname("userNick", "NewName");
            Assert.Equal("NewName", userData.LastSeenNickname);
        }

        [Fact]
        public void UserHasAnyGroup_ReturnsFalseForNewUser()
        {
            Assert.False(permLib.UserHasAnyGroup("userNoGroup"));
        }

        [Fact]
        public void GetUserGroups_ReturnsEmptyForNewUser()
        {
            var groups = permLib.GetUserGroups("userEmpty");
            Assert.Empty(groups);
        }

        [Fact]
        public void GetUserPermissions_ReturnsDirectAndInheritedPermissions()
        {
            // Use the provided FakePlugin.
            FakePlugin plugin = new FakePlugin();
            string directPerm = $"{plugin.Name}.direct";
            string groupPerm = $"{plugin.Name}.group";
            permLib.RegisterPermission(directPerm, plugin);
            permLib.RegisterPermission(groupPerm, plugin);
            string userId = "userUnion";
            permLib.GrantUserPermission(userId, directPerm, plugin);
            bool created = permLib.CreateGroup("unionGroup", "Union Group", 5);
            Assert.True(created);
            permLib.GrantGroupPermission("unionGroup", groupPerm, plugin);
            permLib.AddUserGroup(userId, "unionGroup");
            var perms = permLib.GetUserPermissions(userId);
            Assert.Contains(directPerm, perms);
            Assert.Contains(groupPerm, perms);
        }

        [Fact]
        public void GetGroupPermissions_WithAndWithoutParents()
        {
            FakePlugin plugin = new FakePlugin();
            string parentPerm = $"{plugin.Name}.parent";
            string childPerm = $"{plugin.Name}.child";
            permLib.RegisterPermission(parentPerm, plugin);
            permLib.RegisterPermission(childPerm, plugin);
            bool createdParent = permLib.CreateGroup("parentGroup", "Parent Group", 10);
            bool createdChild = permLib.CreateGroup("childGroup", "Child Group", 1);
            Assert.True(createdParent);
            Assert.True(createdChild);
            permLib.GrantGroupPermission("parentGroup", parentPerm, plugin);
            permLib.GrantGroupPermission("childGroup", childPerm, plugin);
            // Without parent traversal.
            var childOnly = permLib.GetGroupPermissions("childGroup", false);
            Assert.DoesNotContain(parentPerm, childOnly);
            // With parent traversal.
            var withParents = permLib.GetGroupPermissions("childGroup", true);
            Assert.Contains(parentPerm, withParents);
        }

        [Fact]
        public void GetPermissions_ReturnsRegisteredPermissions()
        {
            FakePlugin plugin = new FakePlugin();
            string perm1 = $"{plugin.Name}.perm1";
            string perm2 = $"{plugin.Name}.perm2";
            permLib.RegisterPermission(perm1, plugin);
            permLib.RegisterPermission(perm2, plugin);
            var perms = permLib.GetPermissions();
            Assert.Contains(perm1, perms);
            Assert.Contains(perm2, perms);
        }

        [Fact]
        public void GetPermissionUsers_EmptyPermission_ReturnsEmptyArray()
        {
            var users = permLib.GetPermissionUsers("");
            Assert.Empty(users);
        }

        [Fact]
        public void GetPermissionGroups_EmptyPermission_ReturnsEmptyArray()
        {
            var groups = permLib.GetPermissionGroups("");
            Assert.Empty(groups);
        }

        #endregion

        #region User Group Management Tests

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
        public void UserHasGroup_ReturnsFalseForNonExistingGroup()
        {
            Assert.False(permLib.UserHasGroup("someUser", "nonexistentGroup"));
        }

        #endregion

        #region Group Data and Management Tests

        [Fact]
        public void GetGroupData_ReturnsNullForNonExistingGroup()
        {
            Assert.Null(permLib.GetGroupData("nonexistentGroup"));
        }

        [Fact]
        public void GetGroups_ReturnsCreatedGroups()
        {
            bool created = permLib.CreateGroup("groupTest1", "Group Test 1", 1);
            Assert.True(created);
            var groups = permLib.GetGroups();
            Assert.Contains("groupTest1", groups);
        }

        [Fact]
        public void GetUsersInGroup_ReturnsCorrectUsers()
        {
            bool created = permLib.CreateGroup("groupUsers", "Group Users", 1);
            Assert.True(created);
            string userId = "userGroupRemove";
            permLib.AddUserGroup(userId, "groupUsers");
            var users = permLib.GetUsersInGroup("groupUsers");
            Assert.Contains(users, s => s.Contains(userId));
        }

        [Fact]
        public void GetGroupTitle_ReturnsEmptyForNonExistingGroup()
        {
            Assert.Equal(string.Empty, permLib.GetGroupTitle("nonexistentGroup"));
        }

        [Fact]
        public void GetGroupRank_ReturnsZeroForNonExistingGroup()
        {
            Assert.Equal(0, permLib.GetGroupRank("nonexistentGroup"));
        }

        [Fact]
        public void GetGroupParent_ReturnsProperValue()
        {
            bool createdParent = permLib.CreateGroup("parentGroup", "Parent", 1);
            bool createdChild = permLib.CreateGroup("childGroup", "Child", 1);
            Assert.True(createdParent && createdChild);
            permLib.SetGroupParent("childGroup", "parentGroup");
            Assert.Equal("parentGroup", permLib.GetGroupParent("childGroup"));
            Assert.Equal(string.Empty, permLib.GetGroupParent("nonexistent"));
        }

        [Fact]
        public void SetGroupParent_SetsParentCorrectly()
        {
            bool createdParent = permLib.CreateGroup("groupParent", "Parent", 1);
            bool createdChild = permLib.CreateGroup("groupChild", "Child", 1);
            Assert.True(createdParent && createdChild);
            bool result = permLib.SetGroupParent("groupChild", "groupParent");
            Assert.True(result);
            Assert.Equal("groupParent", permLib.GetGroupParent("groupChild"));
        }

        [Fact]
        public void SetGroupParent_PreventsCircularReference()
        {
            FakePlugin plugin = new FakePlugin();
            bool createdA = permLib.CreateGroup("groupA", "Group A", 1);
            bool createdB = permLib.CreateGroup("groupB", "Group B", 1);
            Assert.True(createdA && createdB);
            bool setParentB = permLib.SetGroupParent("groupB", "groupA");
            Assert.True(setParentB);
            bool setParentA = permLib.SetGroupParent("groupA", "groupB");
            Assert.False(setParentA); // Circular reference should be prevented.
        }

        #endregion

        #region User Permission Management Tests

        [Fact]
        public void GrantUserPermission_Normal_AddsPermission()
        {
            FakePlugin plugin = new FakePlugin();
            string permission = $"{plugin.Name}.normal";
            permLib.RegisterPermission(permission, plugin);
            string userId = "userNormal";
            permLib.GrantUserPermission(userId, permission, plugin);
            Assert.True(permLib.UserHasPermission(userId, permission));
        }

        [Fact]
        public void RevokeUserPermission_Normal_RemovesPermission()
        {
            FakePlugin plugin = new FakePlugin();
            string permission = $"{plugin.Name}.normalRevoke";
            permLib.RegisterPermission(permission, plugin);
            string userId = "userNormalRevoke";
            permLib.GrantUserPermission(userId, permission, plugin);
            permLib.RevokeUserPermission(userId, permission);
            Assert.False(permLib.UserHasPermission(userId, permission));
        }

        [Fact]
        public void GrantUserPermission_Wildcard_AddsAllPermissions()
        {
            FakePlugin plugin = new FakePlugin();
            string perm1 = $"{plugin.Name}.wild1";
            string perm2 = $"{plugin.Name}.wild2";
            permLib.RegisterPermission(perm1, plugin);
            permLib.RegisterPermission(perm2, plugin);
            string userId = "userWildcard";
            permLib.GrantUserPermission(userId, "*", plugin);
            Assert.True(permLib.UserHasPermission(userId, perm1));
            Assert.True(permLib.UserHasPermission(userId, perm2));
        }

        [Fact]
        public void RevokeUserPermission_Wildcard_RemovesAllPermissions()
        {
            FakePlugin plugin = new FakePlugin();
            string perm1 = $"{plugin.Name}.wild1";
            string perm2 = $"{plugin.Name}.wild2";
            permLib.RegisterPermission(perm1, plugin);
            permLib.RegisterPermission(perm2, plugin);
            string userId = "userWildcardRevoke";
            permLib.GrantUserPermission(userId, "*", plugin);
            permLib.RevokeUserPermission(userId, "*");
            Assert.False(permLib.UserHasPermission(userId, perm1));
            Assert.False(permLib.UserHasPermission(userId, perm2));
        }

        #endregion

        #region Group Permission Management Tests

        [Fact]
        public void GrantGroupPermission_Normal_AddsPermissionToGroup()
        {
            FakePlugin plugin = new FakePlugin();
            string permission = $"{plugin.Name}.groupNormal";
            permLib.RegisterPermission(permission, plugin);
            bool created = permLib.CreateGroup("groupNormal", "Group Normal", 1);
            Assert.True(created);
            permLib.GrantGroupPermission("groupNormal", permission, plugin);
            var perms = permLib.GetGroupPermissions("groupNormal");
            Assert.Contains(permission, perms);
        }

        [Fact]
        public void RevokeGroupPermission_Normal_RemovesPermissionFromGroup()
        {
            FakePlugin plugin = new FakePlugin();
            string permission = $"{plugin.Name}.groupNormalRevoke";
            permLib.RegisterPermission(permission, plugin);
            bool created = permLib.CreateGroup("groupNormalRevoke", "Group Normal Revoke", 1);
            Assert.True(created);
            permLib.GrantGroupPermission("groupNormalRevoke", permission, plugin);
            permLib.RevokeGroupPermission("groupNormalRevoke", permission);
            var perms = permLib.GetGroupPermissions("groupNormalRevoke");
            Assert.DoesNotContain(permission, perms);
        }

        [Fact]
        public void GrantGroupPermission_Wildcard_AddsAllPermissionsToGroup()
        {
            FakePlugin plugin = new FakePlugin();
            string perm1 = $"{plugin.Name}.gwild1";
            string perm2 = $"{plugin.Name}.gwild2";
            permLib.RegisterPermission(perm1, plugin);
            permLib.RegisterPermission(perm2, plugin);
            bool created = permLib.CreateGroup("groupWildcard", "Wildcard Group", 1);
            Assert.True(created);
            permLib.GrantGroupPermission("groupWildcard", "*", plugin);
            var groupPerms = permLib.GetGroupPermissions("groupWildcard");
            Assert.Contains(perm1, groupPerms);
            Assert.Contains(perm2, groupPerms);
        }

        [Fact]
        public void RevokeGroupPermission_Wildcard_RemovesAllPermissionsFromGroup()
        {
            FakePlugin plugin = new FakePlugin();
            string perm1 = $"{plugin.Name}.gwild1";
            string perm2 = $"{plugin.Name}.gwild2";
            permLib.RegisterPermission(perm1, plugin);
            permLib.RegisterPermission(perm2, plugin);
            bool created = permLib.CreateGroup("groupWildcard2", "Wildcard Group 2", 1);
            Assert.True(created);
            permLib.GrantGroupPermission("groupWildcard2", "*", plugin);
            permLib.RevokeGroupPermission("groupWildcard2", "*");
            var groupPerms = permLib.GetGroupPermissions("groupWildcard2");
            Assert.DoesNotContain(perm1, groupPerms);
            Assert.DoesNotContain(perm2, groupPerms);
        }

        #endregion

        #region Export, Save and Migrate Tests

        [Fact]
        public void Export_WritesDataFilesWhenLoaded()
        {
            // Use a temporary prefix then check that the files are created.
            string prefix = "testExport";
            permLib.Export(prefix);
            string groupsPath = Path.Combine(tempDataDir, prefix + ".groups");
            string usersPath = Path.Combine(tempDataDir, prefix + ".users");
            Assert.True(File.Exists(groupsPath));
            Assert.True(File.Exists(usersPath));
        }

        [Fact]
        public void SaveData_DoesNotThrowException()
        {
            var ex = Record.Exception(() => permLib.SaveData());
            Assert.Null(ex);
        }

        [Fact]
        public void MigrateGroup_MigratesPermissionsAndRemovesEmptyGroup()
        {
            // Create a dummy oxide.groups.data file.
            string groupsDataPath = Path.Combine(tempDataDir, "oxide.groups.data");
            File.WriteAllText(groupsDataPath, "dummy content");

            bool createdOld = permLib.CreateGroup("oldGroup", "Old Group", 1);
            bool createdNew = permLib.CreateGroup("newGroup", "New Group", 1);
            Assert.True(createdOld && createdNew);

            FakePlugin plugin = new FakePlugin();
            string perm = $"{plugin.Name}.migrate";
            permLib.RegisterPermission(perm, plugin);
            permLib.GrantGroupPermission("oldGroup", perm, plugin);
            // Do not add any user to oldGroup.
            permLib.MigrateGroup("oldGroup", "newGroup");
            var newGroupPerms = permLib.GetGroupPermissions("newGroup");
            Assert.Contains(perm, newGroupPerms);
            Assert.False(permLib.GroupExists("oldGroup"));
            Assert.True(File.Exists(groupsDataPath + ".old"));
        }

        #endregion

        #region Additional Query Tests

        [Fact]
        public void GrantUserPermission_DoesNothingIfPermissionNotRegistered()
        {
            FakePlugin plugin = new FakePlugin();
            string userId = "userNoReg";
            permLib.GrantUserPermission(userId, "nonexistent.permission", plugin);
            Assert.False(permLib.UserHasPermission(userId, "nonexistent.permission"));
        }

        [Fact]
        public void GrantGroupPermission_DoesNothingIfGroupDoesNotExist()
        {
            FakePlugin plugin = new FakePlugin();
            string perm = $"{plugin.Name}.groupNoExist";
            permLib.RegisterPermission(perm, plugin);
            permLib.GrantGroupPermission("nonexistentGroup", perm, plugin);
            var perms = permLib.GetGroupPermissions("nonexistentGroup");
            Assert.Empty(perms);
        }

        [Fact]
        public void RevokeGroupPermission_DoesNothingIfPermissionNotPresent()
        {
            bool created = permLib.CreateGroup("groupRevoke", "Group Revoke", 1);
            Assert.True(created);
            permLib.RevokeGroupPermission("groupRevoke", "nonexistent.permission");
            var perms = permLib.GetGroupPermissions("groupRevoke");
            Assert.Empty(perms);
        }

        [Fact]
        public void RemoveUserGroup_DoesNothingIfUserNotInGroup()
        {
            string userId = "userNotInGroup";
            var userData = permLib.GetUserData(userId);
            userData.Groups.Clear();
            permLib.RemoveUserGroup(userId, "someGroup");
            Assert.Empty(userData.Groups);
        }

        [Fact]
        public void GetPermissionUsers_ReturnsProperlyFormattedUserNames()
        {
            FakePlugin plugin = new FakePlugin();
            string perm = $"{plugin.Name}.formatTest";
            permLib.RegisterPermission(perm, plugin);
            string userId = "userFormat";
            var userData = permLib.GetUserData(userId);
            userData.LastSeenNickname = "Nickname";
            permLib.GrantUserPermission(userId, perm, plugin);
            var users = permLib.GetPermissionUsers(perm);
            Assert.Contains(users, s => s.Contains($"{userId}(Nickname)"));
        }

        [Fact]
        public void GetPermissionGroups_ReturnsCorrectGroupNames()
        {
            FakePlugin plugin = new FakePlugin();
            string perm = $"{plugin.Name}.groupFormat";
            permLib.RegisterPermission(perm, plugin);
            bool created = permLib.CreateGroup("groupFormat", "Group Format", 1);
            Assert.True(created);
            permLib.GrantGroupPermission("groupFormat", perm, plugin);
            var groups = permLib.GetPermissionGroups(perm);
            Assert.Contains("groupFormat", groups);
        }

        #endregion

        public void Dispose()
        {
            // Restore original instance directory.
            var oxide = Interface.Oxide;
            var instanceDirProp = oxide.GetType().GetProperty("InstanceDirectory", BindingFlags.Public | BindingFlags.Instance);
            if (instanceDirProp != null && originalInstanceDir != null)
            {
                instanceDirProp.SetValue(oxide, originalInstanceDir);
            }
            // Delete temporary instance directory.
            if (Directory.Exists(tempInstanceDir))
            {
                Directory.Delete(tempInstanceDir, true);
            }
        }
    }
}
