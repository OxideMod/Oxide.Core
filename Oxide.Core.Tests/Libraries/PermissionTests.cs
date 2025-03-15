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
        }

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
            permLib.SetGroupParent("childGroup", "parentGroup");
            var childOnly = permLib.GetGroupPermissions("childGroup", false);
            Assert.Contains(childPerm, childOnly);
            Assert.DoesNotContain(parentPerm, childOnly);
            var withParents = permLib.GetGroupPermissions("childGroup", true);
            Assert.Contains(parentPerm, withParents);
            Assert.Contains(childPerm, withParents);
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
            bool createdA = permLib.CreateGroup("groupA", "Group A", 1);
            bool createdB = permLib.CreateGroup("groupB", "Group B", 1);
            Assert.True(createdA && createdB);
            bool setParentB = permLib.SetGroupParent("groupB", "groupA");
            Assert.True(setParentB);
            bool setParentA = permLib.SetGroupParent("groupA", "groupB");
            Assert.False(setParentA);
        }

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

        [Fact]
        public void Export_WritesDataFilesWhenLoaded()
        {
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
            string groupsDataPath = Path.Combine(tempDataDir, "oxide.groups.data");
            File.WriteAllText(groupsDataPath, "dummy content");
            bool createdOld = permLib.CreateGroup("oldGroup", "Old Group", 1);
            bool createdNew = permLib.CreateGroup("newGroup", "New Group", 1);
            Assert.True(createdOld && createdNew);
            FakePlugin plugin = new FakePlugin();
            string perm = $"{plugin.Name}.migrate";
            permLib.RegisterPermission(perm, plugin);
            permLib.GrantGroupPermission("oldGroup", perm, plugin);
            permLib.MigrateGroup("oldGroup", "newGroup");
            var newGroupPerms = permLib.GetGroupPermissions("newGroup");
            Assert.Contains(perm, newGroupPerms);
            Assert.False(permLib.GroupExists("oldGroup"));
            Assert.True(File.Exists(groupsDataPath + ".old"));
        }

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

        [Fact]
        public void LoadFromDatafile_HandlesCircularGroupParents()
        {
            var groups = new Dictionary<string, GroupData>
            {
                { "groupA", new GroupData { ParentGroup = "groupB" } },
                { "groupB", new GroupData { ParentGroup = "groupA" } }
            };
            ProtoStorage.Save(groups, "oxide.groups");
            var perm = new Permission();
            var groupA = perm.GetGroupData("groupA");
            var groupB = perm.GetGroupData("groupB");
            Assert.Null(groupA.ParentGroup);
            Assert.Null(groupB.ParentGroup);
            Assert.True(perm.IsLoaded);
        }

        [Fact]
        public void RegisterPermission_DoesNothingWithEmptyPermission()
        {
            FakePlugin plugin = new FakePlugin();
            permLib.RegisterPermission("", plugin);
            Assert.False(permLib.PermissionExists("", plugin));
        }

        [Fact]
        public void PermissionExists_ReturnsFalseForEmptyPermission()
        {
            Assert.False(permLib.PermissionExists(""));
            Assert.False(permLib.PermissionExists("", new FakePlugin()));
        }

        [Fact]
        public void PermissionExists_Wildcard_MatchesStartingPermissions()
        {
            FakePlugin plugin = new FakePlugin();
            permLib.RegisterPermission("plugin.test1", plugin);
            permLib.RegisterPermission("plugin.test2", plugin);
            permLib.RegisterPermission("other.test", plugin);
            Assert.True(permLib.PermissionExists("plugin.*"));
            Assert.False(permLib.PermissionExists("nonexistent.*"));
        }

        [Fact]
        public void PermissionExists_Star_ReturnsTrueIfAnyPermissions()
        {
            FakePlugin plugin = new FakePlugin();
            permLib.RegisterPermission("some.perm", plugin);
            Assert.True(permLib.PermissionExists("*"));
        }

        [Fact]
        public void PermissionExists_Wildcard_NoPermissions_ReturnsFalse()
        {
            var field = typeof(Permission).GetField("registeredPermissions", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(permLib, new Dictionary<Plugin, HashSet<string>>());
            Assert.False(permLib.PermissionExists("*"));
            Assert.False(permLib.PermissionExists("plugin.*"));
        }

        [Fact]
        public void PermissionExists_WithOwner_ChecksOnlyOwnerPermissions()
        {
            var plugin1 = new FakePlugin { Name = "Plugin1" };
            var plugin2 = new FakePlugin { Name = "Plugin2" };
            permLib.RegisterPermission("plugin1.perm", plugin1);
            permLib.RegisterPermission("plugin2.perm", plugin2);
            Assert.True(permLib.PermissionExists("plugin1.perm", plugin1));
            Assert.False(permLib.PermissionExists("plugin1.perm", plugin2));
            Assert.True(permLib.PermissionExists("plugin2.perm", plugin2));
        }

        [Fact]
        public void UserIdValid_NoValidationFunction_ReturnsTrue()
        {
            typeof(Permission).GetField("validate", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(permLib, null);
            Assert.True(permLib.UserIdValid("anyUser"));
        }

        [Fact]
        public void UserIdValid_WithValidationFunction_CallsFunction()
        {
            bool called = false;
            permLib.RegisterValidate(id => { called = true; return id == "validUser"; });
            Assert.True(permLib.UserIdValid("validUser"));
            Assert.False(permLib.UserIdValid("invalidUser"));
            Assert.True(called);
        }

        [Fact]
        public void CleanUp_RemovesInvalidUsers()
        {
            var userData1 = permLib.GetUserData("user1");
            var userData2 = permLib.GetUserData("user2");
            permLib.RegisterValidate(id => id == "user1");
            permLib.CleanUp();
            Assert.True(permLib.UserExists("user1"));
            Assert.False(permLib.UserExists("user2"));
        }

        [Fact]
        public void CleanUp_NoValidationFunction_DoesNothing()
        {
            var userData1 = permLib.GetUserData("userA");
            var userData2 = permLib.GetUserData("userB");
            typeof(Permission).GetField("validate", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(permLib, null);
            permLib.CleanUp();
            Assert.True(permLib.UserExists("userA"));
            Assert.True(permLib.UserExists("userB"));
        }

        [Fact]
        public void MigrateGroup_WithUsers_DoesNotRemoveOldGroup()
        {
            bool createdOld = permLib.CreateGroup("oldGroupWithUsers", "Old Group", 1);
            bool createdNew = permLib.CreateGroup("newGroupWithUsers", "New Group", 1);
            Assert.True(createdOld && createdNew);
            string userId = "userInOldGroup";
            permLib.AddUserGroup(userId, "oldGroupWithUsers");
            FakePlugin plugin = new FakePlugin();
            string perm = $"{plugin.Name}.migrateWithUsers";
            permLib.RegisterPermission(perm, plugin);
            permLib.GrantGroupPermission("oldGroupWithUsers", perm, plugin);
            permLib.MigrateGroup("oldGroupWithUsers", "newGroupWithUsers");
            var newGroupPerms = permLib.GetGroupPermissions("newGroupWithUsers");
            Assert.Contains(perm, newGroupPerms);
            Assert.True(permLib.GroupExists("oldGroupWithUsers"));
            Assert.True(permLib.UserHasGroup(userId, "oldGroupWithUsers"));
        }

        [Fact]
        public void MigrateGroup_NonExistingOldGroup_DoesNothing()
        {
            bool createdNew = permLib.CreateGroup("newGroupNoOld", "New Group", 1);
            Assert.True(createdNew);
            permLib.MigrateGroup("nonexistentOldGroup", "newGroupNoOld");
            var perms = permLib.GetGroupPermissions("newGroupNoOld");
            Assert.Empty(perms);
        }

        [Fact]
        public void CreateGroup_EmptyName_Fails()
        {
            bool result = permLib.CreateGroup("", "Title", 1);
            Assert.False(result);
            Assert.False(permLib.GroupExists(""));
        }

        [Fact]
        public void CreateGroup_NullName_Fails()
        {
            bool result = permLib.CreateGroup(null, "Title", 1);
            Assert.False(result);
            Assert.False(permLib.GroupExists(null));
        }

        [Fact]
        public void RemoveGroup_SetsChildParentsToEmpty()
        {
            bool createdParent = permLib.CreateGroup("parentGroupRemove", "Parent", 1);
            bool createdChild = permLib.CreateGroup("childGroupRemove", "Child", 1);
            Assert.True(createdParent && createdChild);
            permLib.SetGroupParent("childGroupRemove", "parentGroupRemove");
            bool removed = permLib.RemoveGroup("parentGroupRemove");
            Assert.True(removed);
            var childData = permLib.GetGroupData("childGroupRemove");
            Assert.Equal(string.Empty, childData.ParentGroup);
        }

        [Fact]
        public void SetGroupParent_ToItself_Fails()
        {
            bool created = permLib.CreateGroup("selfParentGroup", "Self Parent", 1);
            Assert.True(created);
            bool result = permLib.SetGroupParent("selfParentGroup", "selfParentGroup");
            Assert.False(result);
            var groupData = permLib.GetGroupData("selfParentGroup");
            Assert.Equal(string.Empty, groupData.ParentGroup);
        }

        [Fact]
        public void SetGroupParent_NonExistingParent_Fails()
        {
            bool created = permLib.CreateGroup("groupNoParent", "No Parent", 1);
            Assert.True(created);
            bool result = permLib.SetGroupParent("groupNoParent", "nonexistent");
            Assert.False(result);
            var groupData = permLib.GetGroupData("groupNoParent");
            Assert.Equal(string.Empty, groupData.ParentGroup);
        }

        [Fact]
        public void GrantUserPermission_NonRegistered_DoesNothing()
        {
            string userId = "userNoRegPerm";
            permLib.GrantUserPermission(userId, "nonregistered.perm", null);
            Assert.False(permLib.UserHasPermission(userId, "nonregistered.perm"));
        }

        [Fact]
        public void GrantGroupPermission_NonRegistered_DoesNothing()
        {
            bool created = permLib.CreateGroup("groupNoRegPerm", "No Reg Perm", 1);
            Assert.True(created);
            permLib.GrantGroupPermission("groupNoRegPerm", "nonregistered.perm", null);
            var perms = permLib.GetGroupPermissions("groupNoRegPerm");
            Assert.Empty(perms);
        }

        [Fact]
        public void RevokeUserPermission_NotGranted_DoesNothing()
        {
            string userId = "userNoPerm";
            permLib.RevokeUserPermission(userId, "some.perm");
            Assert.False(permLib.UserHasPermission(userId, "some.perm"));
        }

        [Fact]
        public void Export_WritesCorrectData()
        {
            string userId = "userExport";
            permLib.GetUserData(userId).LastSeenNickname = "ExportUser";
            bool created = permLib.CreateGroup("groupExport", "Export Group", 1);
            Assert.True(created);
            permLib.Export("testExport");
            string groupsPath = Path.Combine(tempDataDir, "testExport.groups");
            string usersPath = Path.Combine(tempDataDir, "testExport.users");
            Assert.True(File.Exists(groupsPath));
            Assert.True(File.Exists(usersPath));
            var groupsData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, GroupData>>("testExport.groups");
            Assert.Contains("groupExport", groupsData.Keys);
            var usersData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, UserData>>("testExport.users");
            Assert.Contains(userId, usersData.Keys);
            Assert.Equal("ExportUser", usersData[userId].LastSeenNickname);
        }

        [Fact]
        public void Permissions_AreCaseInsensitive()
        {
            FakePlugin plugin = new FakePlugin { Name = "CasePlugin" };
            permLib.RegisterPermission("CasePlugin.Perm", plugin);
            Assert.True(permLib.PermissionExists("caseplugin.perm"));
            string userId = "userCase";
            permLib.GrantUserPermission(userId, "caseplugin.perm", plugin);
            Assert.True(permLib.UserHasPermission(userId, "CasePlugin.Perm"));
        }

        [Fact]
        public void Groups_AreCaseInsensitive()
        {
            bool created = permLib.CreateGroup("CaseGroup", "Case Group", 1);
            Assert.True(created);
            Assert.True(permLib.GroupExists("casegroup"));
            string userId = "userCaseGroup";
            permLib.AddUserGroup(userId, "casegroup");
            Assert.True(permLib.UserHasGroup(userId, "CaseGroup"));
        }

        [Fact]
        public void UserIds_AreCaseInsensitive()
        {
            string userIdLower = "userid";
            string userIdUpper = "USERID";
            var userData = permLib.GetUserData(userIdLower);
            userData.LastSeenNickname = "Lower";
            var userDataUpper = permLib.GetUserData(userIdUpper);
            Assert.Equal("Lower", userDataUpper.LastSeenNickname);
        }

        [Fact]
        public void UserHasPermission_ServerConsole_AlwaysTrue()
        {
            Assert.True(permLib.UserHasPermission("server_console", "any.permission"));
        }

        [Fact]
        public void GetUserPermissions_IncludesParentGroupPermissions()
        {
            bool createdParent = permLib.CreateGroup("parentGroupPerm", "Parent", 1);
            bool createdChild = permLib.CreateGroup("childGroupPerm", "Child", 1);
            Assert.True(createdParent && createdChild);
            permLib.SetGroupParent("childGroupPerm", "parentGroupPerm");
            FakePlugin plugin = new FakePlugin();
            string perm = $"{plugin.Name}.parentPerm";
            permLib.RegisterPermission(perm, plugin);
            permLib.GrantGroupPermission("parentGroupPerm", perm, plugin);
            string userId = "userParentPerm";
            permLib.AddUserGroup(userId, "childGroupPerm");
            var userPerms = permLib.GetUserPermissions(userId);
            Assert.Contains(perm, userPerms);
        }

        [Fact]
        public void GroupsHavePermission_ReturnsTrueIfAnyGroupHasPermission()
        {
            FakePlugin plugin = new FakePlugin();
            string perm = $"{plugin.Name}.groupTest";
            permLib.RegisterPermission(perm, plugin);
            bool created = permLib.CreateGroup("groupWithPerm", "Group With Perm", 1);
            Assert.True(created);
            permLib.GrantGroupPermission("groupWithPerm", perm, plugin);
            var groups = new HashSet<string> { "groupWithPerm", "groupWithoutPerm" };
            Assert.True(permLib.GroupsHavePermission(groups, perm));
        }

        [Fact]
        public void GroupHasPermission_ReturnsFalseForNonExistingGroup()
        {
            Assert.False(permLib.GroupHasPermission("nonexistentGroup", "some.perm"));
        }

        [Fact]
        public void SetGroupTitle_ReturnsFalseForNonExistingGroup()
        {
            Assert.False(permLib.SetGroupTitle("nonexistentGroup", "New Title"));
        }

        [Fact]
        public void SetGroupRank_ReturnsFalseForNonExistingGroup()
        {
            Assert.False(permLib.SetGroupRank("nonexistentGroup", 5));
        }

        [Fact]
        public void RemoveGroup_ReturnsFalseForNonExistingGroup()
        {
            Assert.False(permLib.RemoveGroup("nonexistentGroup"));
        }

        [Fact]
        public void HasCircularParent_DetectsCircularReference()
        {
            bool createdA = permLib.CreateGroup("groupCircA", "Group A", 1);
            bool createdB = permLib.CreateGroup("groupCircB", "Group B", 1);
            Assert.True(createdA && createdB);
            bool setParentB = permLib.SetGroupParent("groupCircB", "groupCircA");
            Assert.True(setParentB);
            var hasCircularParentMethod = permLib.GetType().GetMethod("HasCircularParent", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(hasCircularParentMethod);
            bool result = (bool)hasCircularParentMethod.Invoke(permLib, new object[] { "groupCircA", "groupCircB" });
            Assert.True(result);
            bool nonCircularResult = (bool)hasCircularParentMethod.Invoke(permLib, new object[] { "groupCircB", "groupCircA" });
            Assert.False(nonCircularResult);
        }

        [Fact]
        public void SetGroupRank_UpdatesGroupRank()
        {
            bool created = permLib.CreateGroup("groupRank", "Group Rank", 5);
            Assert.True(created);
            bool result = permLib.SetGroupRank("groupRank", 10);
            Assert.True(result);
            Assert.Equal(10, permLib.GetGroupRank("groupRank"));
            bool invalidResult = permLib.SetGroupRank("nonexistentGroup", 15);
            Assert.False(invalidResult);
        }

        [Fact]
        public void RegisterValidate_CleanUp_RemovesInvalidUsers()
        {
            permLib.GetUserData("validUser1");
            permLib.GetUserData("validUser2");
            permLib.GetUserData("invalidUser1");
            permLib.GetUserData("invalidUser2");
            permLib.RegisterValidate(userId => userId.StartsWith("validUser"));
            permLib.CleanUp();
            Assert.NotNull(permLib.GetUserData("validUser1"));
            Assert.NotNull(permLib.GetUserData("validUser2"));
            var usersDataField = permLib.GetType().GetField("usersData", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(usersDataField);
            var usersData = usersDataField.GetValue(permLib) as Dictionary<string, UserData>;
            Assert.NotNull(usersData);
            Assert.False(usersData.ContainsKey("invalidUser1"));
            Assert.False(usersData.ContainsKey("invalidUser2"));
        }

        [Fact]
        public void PermissionExists_ReturnsCorrectResults()
        {
            FakePlugin plugin = new FakePlugin();
            string permission = $"{plugin.Name}.testExists";
            permLib.RegisterPermission(permission, plugin);
            var permissionExistsMethod = permLib.GetType().GetMethod("PermissionExists", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(permissionExistsMethod);
            bool result = (bool)permissionExistsMethod.Invoke(permLib, new object[] { permission, plugin });
            Assert.True(result);
            bool wildcardResult = (bool)permissionExistsMethod.Invoke(permLib, new object[] { "*", plugin });
            Assert.True(wildcardResult);
            bool nonExistingResult = (bool)permissionExistsMethod.Invoke(permLib, new object[] { "nonexistent.permission", plugin });
            Assert.False(nonExistingResult);
        }

        [Fact]
        public void GetPerms_ReturnsAllPermissionsForPlugin()
        {
            FakePlugin plugin = new FakePlugin();
            string perm1 = $"{plugin.Name}.perm1";
            string perm2 = $"{plugin.Name}.perm2";
            permLib.RegisterPermission(perm1, plugin);
            permLib.RegisterPermission(perm2, plugin);
            var getPermsMethod = permLib.GetType().GetMethod("GetPerms", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(getPermsMethod);
            var perms = getPermsMethod.Invoke(permLib, new object[] { plugin }) as HashSet<string>;
            Assert.NotNull(perms);
            Assert.Contains(perm1, perms);
            Assert.Contains(perm2, perms);
            var allPerms = getPermsMethod.Invoke(permLib, new object[] { null }) as HashSet<string>;
            Assert.NotNull(allPerms);
            Assert.Contains(perm1, allPerms);
            Assert.Contains(perm2, allPerms);
        }

        [Fact]
        public void UpdateNickname_UpdatesUserNickname()
        {
            string userId = "userNicknameUpdate";
            permLib.GetUserData(userId).LastSeenNickname = "OldName";
            permLib.UpdateNickname(userId, "NewName");
            Assert.Equal("NewName", permLib.GetUserData(userId).LastSeenNickname);
        }

        [Fact]
        public void GroupsHavePermission_ChecksPermissionsAcrossGroups()
        {
            FakePlugin plugin = new FakePlugin();
            string permission = $"{plugin.Name}.groupTest";
            permLib.RegisterPermission(permission, plugin);
            bool createdA = permLib.CreateGroup("groupA", "Group A", 1);
            bool createdB = permLib.CreateGroup("groupB", "Group B", 2);
            Assert.True(createdA && createdB);
            permLib.GrantGroupPermission("groupB", permission, plugin);
            var groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "groupA", "groupB" };
            var groupsHavePermissionMethod = permLib.GetType().GetMethod("GroupsHavePermission", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(groupsHavePermissionMethod);
            bool result = (bool)groupsHavePermissionMethod.Invoke(permLib, new object[] { groups, permission });
            Assert.True(result);
            bool negativeResult = (bool)groupsHavePermissionMethod.Invoke(permLib, new object[] { groups, "nonexistent.permission" });
            Assert.False(negativeResult);
        }

        [Fact]
        public void UserHasAnyGroup_ReturnsTrueWhenUserHasGroups()
        {
            string userId = "userWithGroups";
            bool created = permLib.CreateGroup("testGroup", "Test Group", 1);
            Assert.True(created);
            permLib.AddUserGroup(userId, "testGroup");
            Assert.True(permLib.UserHasAnyGroup(userId));
        }

        [Fact]
        public void GetGroupPermissions_RecursiveParentGroups()
        {
            FakePlugin plugin = new FakePlugin();
            string permA = $"{plugin.Name}.permA";
            string permB = $"{plugin.Name}.permB";
            string permC = $"{plugin.Name}.permC";
            permLib.RegisterPermission(permA, plugin);
            permLib.RegisterPermission(permB, plugin);
            permLib.RegisterPermission(permC, plugin);
            bool createdA = permLib.CreateGroup("nestedA", "Nested A", 3);
            bool createdB = permLib.CreateGroup("nestedB", "Nested B", 2);
            bool createdC = permLib.CreateGroup("nestedC", "Nested C", 1);
            Assert.True(createdA && createdB && createdC);
            permLib.GrantGroupPermission("nestedA", permA, plugin);
            permLib.GrantGroupPermission("nestedB", permB, plugin);
            permLib.GrantGroupPermission("nestedC", permC, plugin);
            permLib.SetGroupParent("nestedB", "nestedA");
            permLib.SetGroupParent("nestedC", "nestedB");
            var permissions = permLib.GetGroupPermissions("nestedC", true);
            Assert.Contains(permA, permissions);
            Assert.Contains(permB, permissions);
            Assert.Contains(permC, permissions);
        }

        [Fact]
        public void GrantUserPermission_WithNullPlugin_GrantsPermission()
        {
            FakePlugin plugin = new FakePlugin();
            string permission = $"{plugin.Name}.nullTest";
            permLib.RegisterPermission(permission, plugin);
            string userId = "userNullPlugin";
            permLib.GrantUserPermission(userId, permission, null);
            Assert.True(permLib.UserHasPermission(userId, permission));
        }

        [Fact]
        public void GrantGroupPermission_WithNullPlugin_GrantsPermission()
        {
            FakePlugin plugin = new FakePlugin();
            string permission = $"{plugin.Name}.groupNullTest";
            permLib.RegisterPermission(permission, plugin);
            bool created = permLib.CreateGroup("nullPluginGroup", "Null Plugin Group", 1);
            Assert.True(created);
            permLib.GrantGroupPermission("nullPluginGroup", permission, null);
            var perms = permLib.GetGroupPermissions("nullPluginGroup");
            Assert.Contains(permission, perms);
        }

        [Fact]
        public void GrantUserPermission_WithWildcardSubpattern_GrantsMatchingPermissions()
        {
            FakePlugin plugin = new FakePlugin();
            string perm1 = $"{plugin.Name}.sub.perm1";
            string perm2 = $"{plugin.Name}.sub.perm2";
            string perm3 = $"{plugin.Name}.other.perm3";
            permLib.RegisterPermission(perm1, plugin);
            permLib.RegisterPermission(perm2, plugin);
            permLib.RegisterPermission(perm3, plugin);
            string userId = "userSubWildcard";
            permLib.GrantUserPermission(userId, $"{plugin.Name}.sub.*", plugin);
            Assert.True(permLib.UserHasPermission(userId, perm1));
            Assert.True(permLib.UserHasPermission(userId, perm2));
            Assert.False(permLib.UserHasPermission(userId, perm3));
        }

        [Fact]
        public void GrantGroupPermission_WithWildcardSubpattern_GrantsMatchingPermissions()
        {
            FakePlugin plugin = new FakePlugin();
            string perm1 = $"{plugin.Name}.gsub.perm1";
            string perm2 = $"{plugin.Name}.gsub.perm2";
            string perm3 = $"{plugin.Name}.gother.perm3";
            permLib.RegisterPermission(perm1, plugin);
            permLib.RegisterPermission(perm2, plugin);
            permLib.RegisterPermission(perm3, plugin);
            bool created = permLib.CreateGroup("subWildcardGroup", "Sub Wildcard Group", 1);
            Assert.True(created);
            permLib.GrantGroupPermission("subWildcardGroup", $"{plugin.Name}.gsub.*", plugin);
            var perms = permLib.GetGroupPermissions("subWildcardGroup");
            Assert.Contains(perm1, perms);
            Assert.Contains(perm2, perms);
            Assert.DoesNotContain(perm3, perms);
        }

        [Fact]
        public void TryGetGroups_HandlesEmptyData()
        {
            string emptyDataPath = Path.Combine(tempDataDir, "empty.data");
            File.WriteAllText(emptyDataPath, "{}");
            var tryGetGroupsMethod = permLib.GetType().GetMethod("TryGetGroups", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(tryGetGroupsMethod);
            var result = tryGetGroupsMethod.Invoke(null, new object[] { emptyDataPath }) as Dictionary<string, GroupData>;
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void LoadFromDatafile_ExceptionHandling()
        {
            string corruptGroupsPath = Path.Combine(tempDataDir, "oxide.groups");
            File.WriteAllText(corruptGroupsPath, "{invalid_json}");
            var permLibWithCorruptData = new Permission();
            Assert.True(permLibWithCorruptData.IsLoaded);
            File.WriteAllText(corruptGroupsPath, "{}");
        }

        [Fact]
        public void SetGroupParent_NullParent_ClearsParent()
        {
            bool createdParent = permLib.CreateGroup("parentForClear", "Parent For Clear", 2);
            bool createdChild = permLib.CreateGroup("childForClear", "Child For Clear", 1);
            Assert.True(createdParent && createdChild);
            permLib.SetGroupParent("childForClear", "parentForClear");
            Assert.Equal("parentForClear", permLib.GetGroupParent("childForClear"));
            var getGroupDataMethod = typeof(Permission).GetMethod("GetGroupData", BindingFlags.Public | BindingFlags.Instance);
            var childData = getGroupDataMethod.Invoke(permLib, new object[] { "childForClear" });
            var parentGroupProperty = childData.GetType().GetProperty("ParentGroup");
            parentGroupProperty.SetValue(childData, null);
            Assert.Equal(string.Empty, permLib.GetGroupParent("childForClear"));
        }

        [Fact]
        public void VerifyAndLoadUsersData_HandlesCorruptedData()
        {
            string usersFile = Path.Combine(tempDataDir, "oxide.users");
            string backupFile = Path.Combine(tempDataDir, "oxide.users.bak");
            if (File.Exists(usersFile))
            {
                File.Copy(usersFile, backupFile, true);
            }
            try
            {
                string json = @"{
            ""user1"": {
                ""LastSeenNickname"": ""User1"",
                ""Perms"": [""perm1"", ""perm2""],
                ""Groups"": [""group1""]
            },
            ""USER1"": {
                ""LastSeenNickname"": ""USER1"",
                ""Perms"": [""perm3""],
                ""Groups"": [""group2""]
            }
        }";
                File.WriteAllText(usersFile, json);
                var newPermLib = new Permission();
                var userData = newPermLib.GetUserData("user1");
                Assert.Contains("perm1", userData.Perms);
                Assert.Contains("perm3", userData.Perms);
                Assert.Contains("group1", userData.Groups);
                Assert.Contains("group2", userData.Groups);
            }
            finally
            {
                if (File.Exists(backupFile))
                {
                    File.Copy(backupFile, usersFile, true);
                    File.Delete(backupFile);
                }
            }
        }

        [Fact]
        public void VerifyGroupData_MergesDuplicateGroups()
        {
            var inputData = new Dictionary<string, GroupData>(StringComparer.OrdinalIgnoreCase);
            var group1 = new GroupData { Title = "Group 1", Rank = 1 };
            group1.Perms.Add("perm1");
            var group1Dup = new GroupData { Title = "Group 1 Dup", Rank = 2 };
            group1Dup.Perms.Add("perm2");
            inputData.Add("group1", group1);
            inputData.Add("GROUP1", group1Dup);
            var method = typeof(Permission).GetMethod("VerifyGroupData", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = method.Invoke(permLib, new object[] { inputData }) as Dictionary<string, GroupData>;
            Assert.Single(result);
            var mergedGroup = result["group1"];
            Assert.Contains("perm1", mergedGroup.Perms);
            Assert.Contains("perm2", mergedGroup.Perms);
        }

        [Fact]
        public void Owner_OnRemovedFromManager_RemovesPermissions()
        {
            FakePlugin plugin = new FakePlugin();
            string permission = $"{plugin.Name}.removed";
            permLib.RegisterPermission(permission, plugin);
            Assert.True(permLib.PermissionExists(permission));
            var method = typeof(Permission).GetMethod("owner_OnRemovedFromManager", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(permLib, new object[] { plugin });
            Assert.False(permLib.PermissionExists(permission));
        }

        [Fact]
        public void RemoveUserGroup_ComprehensiveTest()
        {
            permLib.CreateGroup("removeGroup1", "Remove Group 1", 1);
            permLib.CreateGroup("removeGroup2", "Remove Group 2", 2);
            string userId = "userGroupRemoveTest";
            permLib.AddUserGroup(userId, "removeGroup1");
            permLib.AddUserGroup(userId, "removeGroup2");
            var userGroups = permLib.GetUserGroups(userId);
            Assert.Contains("removeGroup1", userGroups);
            Assert.Contains("removeGroup2", userGroups);
            permLib.RemoveUserGroup(userId, "removeGroup1");
            userGroups = permLib.GetUserGroups(userId);
            Assert.DoesNotContain("removeGroup1", userGroups);
            Assert.Contains("removeGroup2", userGroups);
            permLib.RemoveUserGroup(userId, "nonExistentGroup");
            permLib.AddUserGroup(userId, "removeGroup1");
            permLib.RemoveUserGroup(userId, "*");
            userGroups = permLib.GetUserGroups(userId);
            Assert.Empty(userGroups);
            permLib.RemoveUserGroup("nonExistentUser", "removeGroup1");
        }

        [Fact]
        public void MigrateGroup_ComprehensiveTests()
        {
            string dataFile = Path.Combine(tempDataDir, "oxide.groups.data");
            File.WriteAllText(dataFile, "dummy content");
            bool sourceCreated = permLib.CreateGroup("sourceGroup", "Source Group", 1);
            bool destCreated = permLib.CreateGroup("destGroup", "Destination Group", 2);
            Assert.True(sourceCreated && destCreated);
            string user1 = "migrateUser1";
            string user2 = "migrateUser2";
            permLib.AddUserGroup(user1, "sourceGroup");
            permLib.AddUserGroup(user2, "sourceGroup");
            FakePlugin plugin = new FakePlugin();
            string perm1 = $"{plugin.Name}.migratePerm1";
            string perm2 = $"{plugin.Name}.migratePerm2";
            permLib.RegisterPermission(perm1, plugin);
            permLib.RegisterPermission(perm2, plugin);
            permLib.GrantGroupPermission("sourceGroup", perm1, plugin);
            permLib.GrantGroupPermission("sourceGroup", perm2, plugin);
            permLib.MigrateGroup("sourceGroup", "destGroup");
            var destPerms = permLib.GetGroupPermissions("destGroup");
            Assert.Contains(perm1, destPerms);
            Assert.Contains(perm2, destPerms);
            Assert.True(permLib.GroupExists("sourceGroup"));
            permLib.MigrateGroup("nonExistentGroup", "destGroup");
            permLib.RemoveUserGroup(user1, "sourceGroup");
            permLib.RemoveUserGroup(user2, "sourceGroup");
            bool emptySourceCreated = permLib.CreateGroup("emptySourceGroup", "Empty Source", 1);
            Assert.True(emptySourceCreated);
            string perm3 = $"{plugin.Name}.migratePerm3";
            permLib.RegisterPermission(perm3, plugin);
            permLib.GrantGroupPermission("emptySourceGroup", perm3, plugin);
            permLib.MigrateGroup("emptySourceGroup", "destGroup");
            Assert.False(permLib.GroupExists("emptySourceGroup"));
            destPerms = permLib.GetGroupPermissions("destGroup");
            Assert.Contains(perm3, destPerms);
        }

        [Fact]
        public void UserHasPermission_AllEdgeCases()
        {
            FakePlugin plugin = new FakePlugin();
            string directPerm = $"{plugin.Name}.directEdge";
            string groupPerm = $"{plugin.Name}.groupEdge";
            string wildcardBase = $"{plugin.Name}.wild";
            string wildcardSub = $"{plugin.Name}.wild.sub";
            permLib.RegisterPermission(directPerm, plugin);
            permLib.RegisterPermission(groupPerm, plugin);
            permLib.RegisterPermission(wildcardBase, plugin);
            permLib.RegisterPermission(wildcardSub, plugin);
            string userId = "permEdgeUser";
            Assert.False(permLib.UserHasPermission("nonExistentUser", directPerm));
            Assert.False(permLib.UserHasPermission(userId, "nonexistent.perm"));
            permLib.GrantUserPermission(userId, directPerm, plugin);
            Assert.True(permLib.UserHasPermission(userId, directPerm));
            bool created = permLib.CreateGroup("edgeGroup", "Edge Group", 1);
            Assert.True(created);
            permLib.GrantGroupPermission("edgeGroup", groupPerm, plugin);
            permLib.AddUserGroup(userId, "edgeGroup");
            Assert.True(permLib.UserHasPermission(userId, groupPerm));
            Assert.False(permLib.UserHasPermission(userId, $"{plugin.Name}.*"));
            permLib.GrantUserPermission(userId, $"{plugin.Name}.*", plugin);
            Assert.True(permLib.UserHasPermission(userId, wildcardBase));
            Assert.True(permLib.UserHasPermission(userId, wildcardSub));
            string userId2 = "permEdgeUser2";
            permLib.GrantUserPermission(userId2, "*", plugin);
            Assert.True(permLib.UserHasPermission(userId2, directPerm));
            Assert.True(permLib.UserHasPermission(userId2, groupPerm));
        }

        [Fact]
        public void GetGroupPermissions_EdgeCases()
        {
            bool created = permLib.CreateGroup("permGroup", "Permission Group", 1);
            Assert.True(created);
            FakePlugin plugin = new FakePlugin();
            string perm = $"{plugin.Name}.groupPermTest";
            permLib.RegisterPermission(perm, plugin);
            permLib.GrantGroupPermission("permGroup", perm, plugin);
            var nonExistentPerms = permLib.GetGroupPermissions("nonExistentGroup", true);
            Assert.Empty(nonExistentPerms);
            bool createdA = permLib.CreateGroup("groupA", "Group A", 3);
            bool createdB = permLib.CreateGroup("groupB", "Group B", 2);
            bool createdC = permLib.CreateGroup("groupC", "Group C", 1);
            Assert.True(createdA && createdB && createdC);
            string permA = $"{plugin.Name}.permA";
            string permB = $"{plugin.Name}.permB";
            string permC = $"{plugin.Name}.permC";
            permLib.RegisterPermission(permA, plugin);
            permLib.RegisterPermission(permB, plugin);
            permLib.RegisterPermission(permC, plugin);
            permLib.GrantGroupPermission("groupA", permA, plugin);
            permLib.GrantGroupPermission("groupB", permB, plugin);
            permLib.GrantGroupPermission("groupC", permC, plugin);
            permLib.SetGroupParent("groupC", "groupB");
            permLib.SetGroupParent("groupB", "groupA");
            var permsC = permLib.GetGroupPermissions("groupC", true);
            Assert.Contains(permA, permsC);
            Assert.Contains(permB, permsC);
            Assert.Contains(permC, permsC);
            var permsNonRecursive = permLib.GetGroupPermissions("groupC", false);
            Assert.DoesNotContain(permA, permsNonRecursive);
            Assert.DoesNotContain(permB, permsNonRecursive);
            Assert.Contains(permC, permsNonRecursive);
        }

        [Fact]
        public void AddUserGroup_EdgeCases()
        {
            string userId = "addGroupEdgeUser";
            permLib.AddUserGroup(userId, "nonExistentGroup");
            var groups = permLib.GetUserGroups(userId);
            Assert.Empty(groups);
            bool created = permLib.CreateGroup("addEdgeGroup", "Add Edge Group", 1);
            Assert.True(created);
            permLib.AddUserGroup(userId, "addEdgeGroup");
            groups = permLib.GetUserGroups(userId);
            Assert.Contains("addEdgeGroup", groups);
            permLib.AddUserGroup(userId, "addEdgeGroup");
            groups = permLib.GetUserGroups(userId);
            Assert.Single(groups);
            permLib.AddUserGroup(userId, "ADDEDGEGROUP");
            groups = permLib.GetUserGroups(userId);
            Assert.Single(groups);
        }

        [Fact]
        public void GroupHasPermission_AllEdgeCases()
        {
            FakePlugin plugin = new FakePlugin();
            string directPerm = $"{plugin.Name}.groupDirectPerm";
            string wildcardBase = $"{plugin.Name}.gwild";
            string wildcardSub = $"{plugin.Name}.gwild.sub";
            permLib.RegisterPermission(directPerm, plugin);
            permLib.RegisterPermission(wildcardBase, plugin);
            permLib.RegisterPermission(wildcardSub, plugin);
            Assert.False(permLib.GroupHasPermission("nonExistentGroup", directPerm));
            bool created = permLib.CreateGroup("groupHasPerm", "Group Has Perm", 1);
            Assert.True(created);
            Assert.False(permLib.GroupHasPermission("groupHasPerm", "nonexistent.perm"));
            permLib.GrantGroupPermission("groupHasPerm", directPerm, plugin);
            Assert.True(permLib.GroupHasPermission("groupHasPerm", directPerm));
            Assert.False(permLib.GroupHasPermission("groupHasPerm", $"{plugin.Name}.*"));
            bool created2 = permLib.CreateGroup("groupHasWild", "Group Has Wild", 1);
            Assert.True(created2);
            permLib.GrantGroupPermission("groupHasWild", $"{plugin.Name}.*", plugin);
            Assert.True(permLib.GroupHasPermission("groupHasWild", wildcardBase));
            Assert.True(permLib.GroupHasPermission("groupHasWild", wildcardSub));
            bool created3 = permLib.CreateGroup("groupHasFullWild", "Group Has Full Wild", 1);
            Assert.True(created3);
            permLib.GrantGroupPermission("groupHasFullWild", "*", plugin);
            Assert.True(permLib.GroupHasPermission("groupHasFullWild", directPerm));
            Assert.True(permLib.GroupHasPermission("groupHasFullWild", wildcardBase));
        }

        [Fact]
        public void IsGlobal_ReturnsFalse()
        {
            bool isGlobal = permLib.IsGlobal;
            Assert.False(isGlobal);
        }

        [Fact]
        public void Owner_OnRemovedFromManager_WithPluginManager_RemovesPermissions()
        {
            FakePlugin plugin = new FakePlugin();
            string permission = $"{plugin.Name}.removedWithManager";
            permLib.RegisterPermission(permission, plugin);
            Assert.True(permLib.PermissionExists(permission));
            var mockPluginManager = new Mock<PluginManager>().Object;
            var method = typeof(Permission).GetMethod("owner_OnRemovedFromManager", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Plugin), typeof(PluginManager) }, null);
            Assert.NotNull(method);
            method.Invoke(permLib, new object[] { plugin, mockPluginManager });
            Assert.False(permLib.PermissionExists(permission));
        }

        [Fact]
        public void VerifyGroupData_DuplicateGroups_MergesCorrectly()
        {
            var inputData = new Dictionary<string, GroupData>(StringComparer.OrdinalIgnoreCase);
            var group1 = new GroupData { Title = "Admin", Rank = 1, Perms = new HashSet<string> { "perm1" } };
            var group2 = new GroupData { Title = "ADMIN", Rank = 2, Perms = new HashSet<string> { "perm2" } };
            inputData.Add("admin", group1);
            inputData.Add("ADMIN", group2);
            var method = typeof(Permission).GetMethod("VerifyGroupData", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = method.Invoke(permLib, new object[] { inputData }) as Dictionary<string, GroupData>;
            Assert.Single(result);
            var mergedGroup = result["admin"];
            Assert.Equal("Admin", mergedGroup.Title);
            Assert.Equal(1, mergedGroup.Rank);
            Assert.Contains("perm1", mergedGroup.Perms);
            Assert.Contains("perm2", mergedGroup.Perms);
        }

        [Fact]
        public void VerifyAndLoadUsersData_MergesDuplicates()
        {
            string usersFile = Path.Combine(tempDataDir, "oxide.users");
            string json = @"{
                ""user1"": {""LastSeenNickname"": ""User1"", ""Perms"": [""perm1""], ""Groups"": [""group1""]},
                ""USER1"": {""LastSeenNickname"": ""USER1"", ""Perms"": [""perm2""], ""Groups"": [""group2""]}
            }";
            File.WriteAllText(usersFile, json);
            var newPermLib = new Permission();
            var userData = newPermLib.GetUserData("user1");
            Assert.Equal("User1", userData.LastSeenNickname);
            Assert.Contains("perm1", userData.Perms);
            Assert.Contains("perm2", userData.Perms);
            Assert.Contains("group1", userData.Groups);
            Assert.Contains("group2", userData.Groups);
        }

        [Fact]
        public void UserIdIsValid_ValidationFunction()
        {
            permLib.RegisterValidate(id => id.Length > 5);
            Assert.True(permLib.UserIdValid("longid"));
            Assert.False(permLib.UserIdValid("short"));
            typeof(Permission).GetField("validate", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(permLib, null);
            Assert.True(permLib.UserIdValid("any"));
        }

        [Fact]
        public void UserHasPermission_EmptyPermission()
        {
            Assert.False(permLib.UserHasPermission("user", ""));
        }

        [Fact]
        public void UserHasGroup_CaseInsensitive()
        {
            string userId = "groupUser";
            permLib.CreateGroup("testGroup", "Test Group", 1);
            permLib.AddUserGroup(userId, "testGroup");
            Assert.True(permLib.UserHasGroup(userId, "TESTGROUP"));
            Assert.False(permLib.UserHasGroup(userId, "otherGroup"));
        }

        [Fact]
        public void Owner_OnRemovedFromManager_Test()
        {
            // Create test plugin
            var fakePlugin = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();

            // Register permission
            string testPermission = "testplugin.permission";
            permLib.RegisterPermission(testPermission, fakePlugin);

            // Verify permission exists
            Assert.True(permLib.PermissionExists(testPermission));

            // Get private method through reflection
            var method = permLib.GetType().GetMethod("owner_OnRemovedFromManager",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Call directly, not using PluginManager
            method.Invoke(permLib, new object[] { fakePlugin, null });

            // Verify permission no longer exists
            Assert.False(permLib.PermissionExists(testPermission));
        }

        [Fact]
        public void VerifyAndLoadUsersData_CorruptedData()
        {
            // Write corrupted users data to file
            string usersFile = Path.Combine(tempDataDir, "oxide.users.data");
            File.WriteAllText(usersFile, "This is not valid protobuf data");

            // Create new permission instance which should handle the corrupted data
            var newPermLib = new Permission();

            // Verify it still loads without throwing exception
            Assert.True(newPermLib.IsLoaded);

            // Add a user to verify basic functionality still works
            newPermLib.GrantUserPermission("testuser", "test.permission", null);
            Assert.True(newPermLib.UserExists("testuser"));
        }

        [Fact]
        public void VerifyAndLoadGroupsData_CorruptedData()
        {
            // Write corrupted groups data to file
            string groupsFile = Path.Combine(tempDataDir, "oxide.groups.data");
            File.WriteAllText(groupsFile, "This is not valid protobuf data");

            // Create new permission instance which should handle the corrupted data
            var newPermLib = new Permission();

            // Verify it still loads without throwing exception
            Assert.True(newPermLib.IsLoaded);

            // Add a group to verify basic functionality still works
            newPermLib.CreateGroup("testgroup", "Test Group", 1);
            Assert.True(newPermLib.GroupExists("testgroup"));
        }

        [Fact]
        public void GetUserGroupPermissions_ComplexTest()
        {
            // Setup complex parent group hierarchy for testing
            permLib.CreateGroup("parent", "Parent Group", 100);
            permLib.CreateGroup("child", "Child Group", 50);
            permLib.CreateGroup("grandchild", "Grandchild Group", 25);

            // Set parent relationships
            permLib.SetGroupParent("child", "parent");
            permLib.SetGroupParent("grandchild", "child");

            // Add permissions to each level
            permLib.GrantGroupPermission("parent", "parent.permission", null);
            permLib.GrantGroupPermission("child", "child.permission", null);
            permLib.GrantGroupPermission("grandchild", "grandchild.permission", null);

            // Create a user and add to the lowest group
            string userId = "hierarchyUser";
            permLib.AddUserGroup(userId, "grandchild");

            // Test inheritance through multiple levels
            var userPerms = permLib.GetUserPermissions(userId);

            // User should have all permissions from the full hierarchy
            Assert.Contains("parent.permission", userPerms);
            Assert.Contains("child.permission", userPerms);
            Assert.Contains("grandchild.permission", userPerms);

            // Test a user with direct permissions
            string directUser = "directPermUser";
            permLib.GrantUserPermission(directUser, "direct.permission", null);
            permLib.AddUserGroup(directUser, "grandchild");

            userPerms = permLib.GetUserPermissions(directUser);

            // User should have direct and all inherited permissions
            Assert.Contains("direct.permission", userPerms);
            Assert.Contains("parent.permission", userPerms);
            Assert.Contains("child.permission", userPerms);
            Assert.Contains("grandchild.permission", userPerms);
        }

        [Fact]
        public void HasCircularParent_ComplexCycles()
        {
            // Create test groups
            permLib.CreateGroup("a", "Group A", 1);
            permLib.CreateGroup("b", "Group B", 2);
            permLib.CreateGroup("c", "Group C", 3);
            permLib.CreateGroup("d", "Group D", 4);

            // Set up complex relationships
            permLib.SetGroupParent("b", "a");
            permLib.SetGroupParent("c", "b");
            permLib.SetGroupParent("d", "c");

            // Get HasCircularParent method through reflection
            var methodInfo = permLib.GetType().GetMethod("HasCircularParent",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Test cycle detection with different levels of indirection

            // Would create: a -> d -> c -> b -> a (circular)
            var result1 = (bool)methodInfo.Invoke(permLib, new object[] { "a", "d" });
            Assert.True(result1);

            // Would create: b -> c -> b (circular)
            var result2 = (bool)methodInfo.Invoke(permLib, new object[] { "b", "c" });
            Assert.True(result2);

            // Creating another group to test non-circular case
            permLib.CreateGroup("e", "Group E", 5);

            // Would create: e -> a (non-circular)
            var result3 = (bool)methodInfo.Invoke(permLib, new object[] { "e", "a" });
            Assert.False(result3);
        }

        [Fact]
        public void GrantUserPermission_AllCases()
        {
            // Create a test plugin
            var fakePlugin = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();

            // Register some permissions
            permLib.RegisterPermission("test.perm1", fakePlugin);
            permLib.RegisterPermission("test.subperm.one", fakePlugin);
            permLib.RegisterPermission("test.subperm.two", fakePlugin);
            permLib.RegisterPermission("other.permission", fakePlugin);

            // Test with unregistered permission - should do nothing
            string userId = "testUser";
            permLib.GrantUserPermission(userId, "nonexistent.permission", fakePlugin);
            var userPerms = permLib.GetUserPermissions(userId);
            Assert.DoesNotContain("nonexistent.permission", userPerms);

            // Test with wildcard permission
            permLib.GrantUserPermission(userId, "test.*", fakePlugin);
            userPerms = permLib.GetUserPermissions(userId);
            Assert.Contains("test.perm1", userPerms);
            Assert.Contains("test.subperm.one", userPerms);
            Assert.Contains("test.subperm.two", userPerms);
            Assert.DoesNotContain("other.permission", userPerms);

            // Test with sub-wildcard
            string user2 = "testUser2";
            permLib.GrantUserPermission(user2, "test.subperm.*", fakePlugin);
            var user2Perms = permLib.GetUserPermissions(user2);
            Assert.Contains("test.subperm.one", user2Perms);
            Assert.Contains("test.subperm.two", user2Perms);
            Assert.DoesNotContain("test.perm1", user2Perms);

            // Test granting a permission that's already granted
            int beforeCount = permLib.GetUserPermissions(user2).Length;
            permLib.GrantUserPermission(user2, "test.subperm.one", fakePlugin);
            int afterCount = permLib.GetUserPermissions(user2).Length;
            Assert.Equal(beforeCount, afterCount); // Count shouldn't change

            // Test with global wildcard
            string user3 = "testUser3";
            permLib.GrantUserPermission(user3, "*", fakePlugin);
            var user3Perms = permLib.GetUserPermissions(user3);
            Assert.Contains("test.perm1", user3Perms);
            Assert.Contains("test.subperm.one", user3Perms);
            Assert.Contains("test.subperm.two", user3Perms);
            Assert.Contains("other.permission", user3Perms);
        }

        [Fact]
        public void GrantGroupPermission_AllCases()
        {
            // Create a test plugin
            var fakePlugin = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();

            // Register some permissions
            permLib.RegisterPermission("test.perm1", fakePlugin);
            permLib.RegisterPermission("test.subperm.one", fakePlugin);
            permLib.RegisterPermission("test.subperm.two", fakePlugin);
            permLib.RegisterPermission("other.permission", fakePlugin);

            // Create a group
            permLib.CreateGroup("testgroup", "Test Group", 1);

            // Test with unregistered permission - should do nothing
            permLib.GrantGroupPermission("testgroup", "nonexistent.permission", fakePlugin);
            var groupPerms = permLib.GetGroupPermissions("testgroup");
            Assert.DoesNotContain("nonexistent.permission", groupPerms);

            // Test with non-existent group - should do nothing
            permLib.GrantGroupPermission("nonexistentgroup", "test.perm1", fakePlugin);

            // Test with wildcard permission
            permLib.GrantGroupPermission("testgroup", "test.*", fakePlugin);
            groupPerms = permLib.GetGroupPermissions("testgroup");
            Assert.Contains("test.perm1", groupPerms);
            Assert.Contains("test.subperm.one", groupPerms);
            Assert.Contains("test.subperm.two", groupPerms);
            Assert.DoesNotContain("other.permission", groupPerms);

            // Test with global wildcard
            permLib.CreateGroup("admingroup", "Admin Group", 100);
            permLib.GrantGroupPermission("admingroup", "*", fakePlugin);
            var adminPerms = permLib.GetGroupPermissions("admingroup");
            Assert.Contains("test.perm1", adminPerms);
            Assert.Contains("test.subperm.one", adminPerms);
            Assert.Contains("test.subperm.two", adminPerms);
            Assert.Contains("other.permission", adminPerms);

            // Test permission already granted - should not add duplicate
            int beforeCount = permLib.GetGroupPermissions("admingroup").Length;
            permLib.GrantGroupPermission("admingroup", "test.perm1", fakePlugin);
            int afterCount = permLib.GetGroupPermissions("admingroup").Length;
            Assert.Equal(beforeCount, afterCount); // Count shouldn't change
        }

        [Fact]
        public void RevokeUserPermission_CompleteTest()
        {
            // Create a test user with several permissions
            string userId = "revokeUser";
            permLib.GrantUserPermission(userId, "test.permission1", null);
            permLib.GrantUserPermission(userId, "test.permission2", null);
            permLib.GrantUserPermission(userId, "other.permission", null);

            // Verify initial state
            var perms = permLib.GetUserPermissions(userId);
            Assert.Contains("test.permission1", perms);
            Assert.Contains("test.permission2", perms);
            Assert.Contains("other.permission", perms);

            // Test revoking a specific permission
            permLib.RevokeUserPermission(userId, "test.permission1");
            perms = permLib.GetUserPermissions(userId);
            Assert.DoesNotContain("test.permission1", perms);
            Assert.Contains("test.permission2", perms);

            // Test revoking with wildcard
            permLib.RevokeUserPermission(userId, "test.*");
            perms = permLib.GetUserPermissions(userId);
            Assert.DoesNotContain("test.permission1", perms);
            Assert.DoesNotContain("test.permission2", perms);
            Assert.Contains("other.permission", perms);

            // Test revoking a permission that doesn't exist - should do nothing
            permLib.RevokeUserPermission(userId, "nonexistent.permission");
            perms = permLib.GetUserPermissions(userId);
            Assert.Contains("other.permission", perms);

            // Test revoking all permissions
            permLib.RevokeUserPermission(userId, "*");
            perms = permLib.GetUserPermissions(userId);
            Assert.Empty(perms);

            // Test revoking from empty permission set - should do nothing
            permLib.RevokeUserPermission(userId, "any.permission");

            // Test with empty permission string - should do nothing
            permLib.GrantUserPermission(userId, "test.permission", null);
            permLib.RevokeUserPermission(userId, "");
            perms = permLib.GetUserPermissions(userId);
            Assert.Contains("test.permission", perms);
        }

        [Fact]
        public void RevokeGroupPermission_CompleteTest()
        {
            // Create a test group with several permissions
            string groupName = "revokeGroup";
            permLib.CreateGroup(groupName, "Revoke Test Group", 1);
            permLib.GrantGroupPermission(groupName, "test.permission1", null);
            permLib.GrantGroupPermission(groupName, "test.permission2", null);
            permLib.GrantGroupPermission(groupName, "other.permission", null);

            // Verify initial state
            var perms = permLib.GetGroupPermissions(groupName);
            Assert.Contains("test.permission1", perms);
            Assert.Contains("test.permission2", perms);
            Assert.Contains("other.permission", perms);

            // Test revoking a specific permission
            permLib.RevokeGroupPermission(groupName, "test.permission1");
            perms = permLib.GetGroupPermissions(groupName);
            Assert.DoesNotContain("test.permission1", perms);
            Assert.Contains("test.permission2", perms);

            // Test revoking with wildcard
            permLib.RevokeGroupPermission(groupName, "test.*");
            perms = permLib.GetGroupPermissions(groupName);
            Assert.DoesNotContain("test.permission1", perms);
            Assert.DoesNotContain("test.permission2", perms);
            Assert.Contains("other.permission", perms);

            // Test revoking a permission that doesn't exist - should do nothing
            permLib.RevokeGroupPermission(groupName, "nonexistent.permission");
            perms = permLib.GetGroupPermissions(groupName);
            Assert.Contains("other.permission", perms);

            // Test revoking all permissions
            permLib.RevokeGroupPermission(groupName, "*");
            perms = permLib.GetGroupPermissions(groupName);
            Assert.Empty(perms);

            // Test with non-existent group - should do nothing
            permLib.RevokeGroupPermission("nonexistentgroup", "any.permission");

            // Test with empty permission string - should do nothing
            permLib.GrantGroupPermission(groupName, "test.permission", null);
            permLib.RevokeGroupPermission(groupName, "");
            perms = permLib.GetGroupPermissions(groupName);
            Assert.Contains("test.permission", perms);
        }

        [Fact]
        public void GetGroupParent_CompleteTest()
        {
            // Test with null/empty parent
            permLib.CreateGroup("emptyParentGroup", "Empty Parent Group", 1);
            Assert.Equal(string.Empty, permLib.GetGroupParent("emptyParentGroup"));

            // Test with existing parent
            permLib.CreateGroup("parentGroup", "Parent Group", 2);
            permLib.CreateGroup("childGroup", "Child Group", 1);
            permLib.SetGroupParent("childGroup", "parentGroup");
            Assert.Equal("parentGroup", permLib.GetGroupParent("childGroup"));

            // Test with different case
            Assert.Equal("parentGroup", permLib.GetGroupParent("CHILDGROUP"));

            // Test with non-existent group
            Assert.Equal(string.Empty, permLib.GetGroupParent("nonexistentGroup"));
        }

        [Fact]
        public void GetGroupTitle_ExistingGroup()
        {
            permLib.CreateGroup("titleGroup", "Group Title", 1);
            Assert.Equal("Group Title", permLib.GetGroupTitle("titleGroup"));
        }

        [Fact]
        public void SetGroupTitle_UpdatesTitle()
        {
            permLib.CreateGroup("titleGroup", "Old Title", 1);
            permLib.SetGroupTitle("titleGroup", "New Title");
            Assert.Equal("New Title", permLib.GetGroupTitle("titleGroup"));
        }

        [Fact]
        public void SetGroupParent_ExistingParent()
        {
            permLib.CreateGroup("parent", "Parent", 1);
            permLib.CreateGroup("child", "Child", 2);
            Assert.True(permLib.SetGroupParent("child", "parent"));
            Assert.Equal("parent", permLib.GetGroupParent("child"));
        }

        [Fact]
        public void RevokeUserPermission_SubWildcard()
        {
            FakePlugin plugin = new FakePlugin();
            permLib.RegisterPermission("plugin.sub.perm1", plugin);
            permLib.RegisterPermission("plugin.sub.perm2", plugin);
            permLib.GrantUserPermission("user", "plugin.sub.perm1", plugin);
            permLib.GrantUserPermission("user", "plugin.sub.perm2", plugin);
            permLib.RevokeUserPermission("user", "plugin.sub.*");
            Assert.False(permLib.UserHasPermission("user", "plugin.sub.perm1"));
            Assert.False(permLib.UserHasPermission("user", "plugin.sub.perm2"));
        }

        [Fact]
        public void RegisterPermission_DuplicateWarning()
        {
            FakePlugin plugin = new FakePlugin();
            permLib.RegisterPermission("plugin.test", plugin);
            permLib.RegisterPermission("plugin.test", plugin);
            Assert.True(permLib.PermissionExists("plugin.test"));
        }

        [Fact]
        public void RegisterPermission_PrefixWarning()
        {
            FakePlugin plugin = new FakePlugin { Name = "TestPlugin" };
            permLib.RegisterPermission("wrongprefix.test", plugin);
            Assert.True(permLib.PermissionExists("wrongprefix.test"));
        }

        [Fact]
        public void RemoveGroup_UpdatesUsers()
        {
            permLib.CreateGroup("removeGroup", "Remove Group", 1);
            permLib.AddUserGroup("user1", "removeGroup");
            permLib.AddUserGroup("user2", "removeGroup");
            permLib.RemoveGroup("removeGroup");
            Assert.False(permLib.UserHasGroup("user1", "removeGroup"));
            Assert.False(permLib.UserHasGroup("user2", "removeGroup"));
        }

        [Fact]
        public void GetUsersInGroup_EmptyGroup()
        {
            permLib.CreateGroup("emptyGroup", "Empty Group", 1);
            var users = permLib.GetUsersInGroup("emptyGroup");
            Assert.Empty(users);
        }

        [Fact]
        public void GetUsersInGroup_NonExistingGroup()
        {
            var users = permLib.GetUsersInGroup("nonExisting");
            Assert.Empty(users);
        }

        [Fact]
        public void GetGroupRank_ExistingGroup()
        {
            permLib.CreateGroup("rankGroup", "Rank Group", 10);
            Assert.Equal(10, permLib.GetGroupRank("rankGroup"));
        }

        [Fact]
        public void VerifyAndLoadGroupsData_MergesDuplicates()
        {
            string groupsFile = Path.Combine(tempDataDir, "oxide.groups");
            string json = @"{
                ""group1"": {""Title"": ""Group1"", ""Rank"": 1, ""Perms"": [""perm1""]},
                ""GROUP1"": {""Title"": ""GROUP1"", ""Rank"": 2, ""Perms"": [""perm2""]}
            }";
            File.WriteAllText(groupsFile, json);
            var newPermLib = new Permission();
            var groupData = newPermLib.GetGroupData("group1");
            Assert.Equal("Group1", groupData.Title);
            Assert.Equal(1, groupData.Rank);
            Assert.Contains("perm1", groupData.Perms);
            Assert.Contains("perm2", groupData.Perms);
        }

        [Fact]
        public void LoadFromDatafile_InitializesCorrectly()
        {
            var newPermLib = new Permission();
            Assert.True(newPermLib.IsLoaded);
        }

        [Fact]
        public void UserExists_ReturnsTrueForExistingUser()
        {
            // Add user to make sure they exist
            permLib.AddUserGroup("testuser123", "admin");

            // Check if user exists
            Assert.True(permLib.UserExists("testuser123"));

            // Check non-existent user
            Assert.False(permLib.UserExists("nonexistentuser"));
        }

        [Fact]
        public void HasCircularParent_DirectCycle()
        {
            // Setup two groups that directly reference each other
            permLib.CreateGroup("groupA", "Group A", 0);
            permLib.CreateGroup("groupB", "Group B", 0);

            // Set B as parent of A
            permLib.SetGroupParent("groupA", "groupB");

            // Try to set A as parent of B (would create a cycle)
            bool result = permLib.SetGroupParent("groupB", "groupA");

            // Should fail because it would create a cycle
            Assert.False(result);
        }

        [Fact]
        public void HasCircularParent_LongerCycle()
        {
            // Setup a longer chain: A -> B -> C -> D -> (try to point to A)
            permLib.CreateGroup("groupA", "Group A", 0);
            permLib.CreateGroup("groupB", "Group B", 0);
            permLib.CreateGroup("groupC", "Group C", 0);
            permLib.CreateGroup("groupD", "Group D", 0);

            permLib.SetGroupParent("groupA", "groupB");
            permLib.SetGroupParent("groupB", "groupC");
            permLib.SetGroupParent("groupC", "groupD");

            // This would create a cycle: A -> B -> C -> D -> A
            bool result = permLib.SetGroupParent("groupD", "groupA");

            // Should fail because it would create a cycle
            Assert.False(result);
        }

        [Fact]
        public void TryGetGroups_WithValidData()
        {
            // Create a test group
            permLib.CreateGroup("testgroup", "Test Group", 10);

            // Export groups to test data retrieval
            permLib.Export("test_export");

            // Create a new permission instance to test loading
            var newPermLib = new Permission();

            // It should successfully load the groups we exported
            Assert.Contains("testgroup", newPermLib.GetGroups());
        }

        [Fact]
        public void VerifyGroupData_HandlesNullPerms()
        {
            // Create a group
            permLib.CreateGroup("nullpermsgroup", "Null Perms Group", 1);

            // Use reflection to access the private method
            var verifyGroupDataMethod = typeof(Permission).GetMethod("VerifyGroupData",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Create test data with null perms
            var testData = new Dictionary<string, GroupData>(StringComparer.OrdinalIgnoreCase)
            {
                ["nullpermsgroup"] = new GroupData { Title = "Test", Rank = 1, Perms = null }
            };

            // Call the method
            var result = verifyGroupDataMethod.Invoke(permLib, new[] { testData }) as Dictionary<string, GroupData>;

            // The method should handle null Perms and create an empty collection
            Assert.NotNull(result);
            Assert.NotNull(result["nullpermsgroup"].Perms);
            Assert.Empty(result["nullpermsgroup"].Perms);
        }

        [Fact]
        public void UserExists_CaseInsensitive()
        {
            // Add user with specific casing
            permLib.UpdateNickname("TestUser", "TestUserName");

            // Check with different casing
            Assert.True(permLib.UserExists("testuser"));
            Assert.True(permLib.UserExists("TESTUSER"));
        }

        //[Fact]
        //public void Owner_OnRemovedFromManager_RemovesPermissionsCompletely()
        //{
        //    // Create a test plugin
        //    var plugin = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();

        //    // Register a permission
        //    string testPerm = "test.permission.forremoval";
        //    permLib.RegisterPermission(testPerm, plugin);

        //    // Verify permission exists
        //    Assert.True(permLib.PermissionExists(testPerm));

        //    // Trigger the removal handler directly using reflection
        //    var method = typeof(Permission).GetMethod("owner_OnRemovedFromManager",
        //        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        //    var pluginManager = new PluginManager(Interface.Oxide);
        //    method.Invoke(permLib, new object[] { plugin, pluginManager });

        //    // Verify permission was removed
        //    Assert.False(permLib.PermissionExists(testPerm));
        //}

        [Fact]
        public void RegisterPermission_WithSpecialCharacters()
        {
            // Create a test plugin
            var plugin = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();

            // Register permissions with special characters
            string specialPerm = "test.permission-with.special_chars";
            permLib.RegisterPermission(specialPerm, plugin);

            // Verify permission exists
            Assert.True(permLib.PermissionExists(specialPerm));
        }

        [Fact]
        public void Export_WithEmptyPermissions()
        {
            // Create a fresh permission instance
            var emptyPermLib = new Permission();

            // Export with no data
            string testPrefix = "emptytest";
            emptyPermLib.Export(testPrefix);

            // Files should still be created
            string usersFile = Path.Combine(tempDataDir, $"{testPrefix}.users.json");
            string groupsFile = Path.Combine(tempDataDir, $"{testPrefix}.groups.json");

            Assert.True(File.Exists(usersFile));
            Assert.True(File.Exists(groupsFile));
        }

        [Fact]
        public void UserIdValid_AllScenarios()
        {
            // With no validation function, should always return true
            Assert.True(permLib.UserIdValid("anyuserid"));

            // Register a validation function that only accepts IDs starting with "valid"
            permLib.RegisterValidate(id => id.StartsWith("valid"));

            // Test with the validation function
            Assert.True(permLib.UserIdValid("valid123"));
            Assert.False(permLib.UserIdValid("invalid123"));

            // Clean up - reset the validation function
            permLib.RegisterValidate(null);
        }

        //[Fact]
        //public void GetPerms_WithNullPlugin()
        //{
        //    // Register permissions from different plugins
        //    var plugin1 = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();
        //    permLib.RegisterPermission("test.perm1", plugin1);
        //    permLib.RegisterPermission("test.perm2", plugin1);

        //    // Get permissions with null plugin (should return all permissions)
        //    string[] allPerms = permLib.GetPerms(null);

        //    // Should contain all permissions regardless of plugin
        //    Assert.Contains("test.perm1", allPerms);
        //    Assert.Contains("test.perm2", allPerms);
        //}

        [Fact]
        public void VerifyGroupData_MergesDuplicatesWithDifferentCasing()
        {
            // Use reflection to access the private method
            var verifyGroupDataMethod = typeof(Permission).GetMethod("VerifyGroupData",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Create test data with duplicate groups but different casing
            var testData = new Dictionary<string, GroupData>(StringComparer.OrdinalIgnoreCase)
            {
                ["testgroup"] = new GroupData
                {
                    Title = "Test Group 1",
                    Rank = 1,
                    Perms = new HashSet<string>(new[] { "perm1" }, StringComparer.OrdinalIgnoreCase)
                },
                ["TestGroup"] = new GroupData
                {
                    Title = "Test Group 2",
                    Rank = 2,
                    Perms = new HashSet<string>(new[] { "perm2" }, StringComparer.OrdinalIgnoreCase)
                }
            };

            // Call the method
            var result = verifyGroupDataMethod.Invoke(permLib, new[] { testData }) as Dictionary<string, GroupData>;

            // Should merge permissions from both groups
            Assert.Single(result);
            var mergedGroup = result.First().Value;
            Assert.Contains("perm1", mergedGroup.Perms);
            Assert.Contains("perm2", mergedGroup.Perms);
        }

        //[Fact]
        //public void Call_ExtensiveHookTests()
        //{
        //    // Test calling hooks from the permission system

        //    // Setup a test object to monitor hooks
        //    bool hookCalled = false;
        //    string calledHook = null;
        //    string playerId = null;
        //    string permission = null;

        //    // Create a test plugin to register permissions
        //    var plugin = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();
        //    permLib.RegisterPermission("test.hook.permission", plugin);

        //    // Setup hook handlers using reflection to access private fields
        //    var hooksField = typeof(OxideMod).GetField("hooks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        //    var hooks = hooksField.GetValue(Interface.Oxide) as Dictionary<string, List<HookMethod>>;

        //    // Mock a hook handler
        //    Action<string, string> hookHandler = (id, perm) =>
        //    {
        //        hookCalled = true;
        //        calledHook = "OnUserPermissionGranted";
        //        playerId = id;
        //        permission = perm;
        //    };

        //    // Register hook handler
        //    Interface.Oxide.OnHookAdded += (name) => { };

        //    // Grant permission to trigger hook
        //    permLib.GrantUserPermission("testuser", "test.hook.permission", plugin);

        //    // Verify hook was processed (through normal API)
        //    permLib.SaveData();
        //    Assert.True(permLib.UserHasPermission("testuser", "test.hook.permission"));
        //}

        [Fact]
        public void VerifyAndLoadUsersData_HandlesEmptyFile()
        {
            // Delete the users data file if it exists
            string usersFile = Path.Combine(tempDataDir, "oxide.users.data");
            if (File.Exists(usersFile))
            {
                File.Delete(usersFile);
            }

            // Create a new permission instance which will try to load the data
            var newPermLib = new Permission();

            // Should not throw an exception and should initialize with empty data
            Assert.Empty(newPermLib.GetUserPermissions("newuser"));
        }

        [Fact]
        public void VerifyAndLoadGroupsData_HandlesEmptyFile()
        {
            // Delete the groups data file if it exists
            string groupsFile = Path.Combine(tempDataDir, "oxide.groups.data");
            if (File.Exists(groupsFile))
            {
                File.Delete(groupsFile);
            }

            // Create a new permission instance which will try to load the data
            var newPermLib = new Permission();

            // Should not throw an exception and should initialize with empty data
            Assert.Empty(newPermLib.GetGroups());
        }

        [Fact]
        public void Call_AllHookTypes()
        {
            // Create a test plugin
            var plugin = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();

            // Register and test various hook scenarios

            // 1. Group creation hooks
            permLib.CreateGroup("callhookgroup", "Call Hook Group", 5);

            // 2. User permission hooks
            permLib.RegisterPermission("callhook.test", plugin);
            permLib.GrantUserPermission("callhookuser", "callhook.test", plugin);
            permLib.RevokeUserPermission("callhookuser", "callhook.test");

            // 3. Group permission hooks
            permLib.GrantGroupPermission("callhookgroup", "callhook.test", plugin);
            permLib.RevokeGroupPermission("callhookgroup", "callhook.test");

            // 4. User group hooks
            permLib.AddUserGroup("callhookuser", "callhookgroup");
            permLib.RemoveUserGroup("callhookuser", "callhookgroup");

            // 5. Group property hooks
            permLib.SetGroupTitle("callhookgroup", "New Title");
            permLib.SetGroupRank("callhookgroup", 10);
            permLib.SetGroupParent("callhookgroup", null);

            // 6. Group deletion hooks
            permLib.RemoveGroup("callhookgroup");

            // Verify the hooks were processed indirectly by checking the state
            Assert.False(permLib.GroupExists("callhookgroup"));
            Assert.False(permLib.UserHasPermission("callhookuser", "callhook.test"));
        }

        [Fact]
        public void LoadFromDatafile_WithCorruptedFiles()
        {
            // Create corrupted data files
            string usersFile = Path.Combine(tempDataDir, "oxide.users.data");
            string groupsFile = Path.Combine(tempDataDir, "oxide.groups.data");

            // Write invalid data to the files
            File.WriteAllText(usersFile, "This is not valid protobuf data");
            File.WriteAllText(groupsFile, "This is not valid protobuf data either");

            // Create a new permission instance which will try to load the data
            var newPermLib = new Permission();

            // Should not throw an exception and should initialize with empty data
            Assert.Empty(newPermLib.GetGroups());
            Assert.Empty(newPermLib.GetUserPermissions("newuser"));
        }

        //[Fact]
        //public void CompleteEdgeCaseScenario()
        //{
        //    // Test multiple edge cases in one comprehensive test

        //    // 1. Create a complex group hierarchy
        //    permLib.CreateGroup("rootgroup", "Root Group", 100);
        //    permLib.CreateGroup("childgroup1", "Child Group 1", 50);
        //    permLib.CreateGroup("childgroup2", "Child Group 2", 50);
        //    permLib.CreateGroup("grandchildgroup", "Grandchild Group", 25);

        //    // 2. Set up hierarchy
        //    permLib.SetGroupParent("childgroup1", "rootgroup");
        //    permLib.SetGroupParent("childgroup2", "rootgroup");
        //    permLib.SetGroupParent("grandchildgroup", "childgroup1");

        //    // 3. Register permissions
        //    var plugin = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();
        //    permLib.RegisterPermission("complex.test1", plugin);
        //    permLib.RegisterPermission("complex.test2", plugin);
        //    permLib.RegisterPermission("complex.test3", plugin);

        //    // 4. Add permissions to groups
        //    permLib.GrantGroupPermission("rootgroup", "complex.test1", plugin);
        //    permLib.GrantGroupPermission("childgroup1", "complex.test2", plugin);
        //    permLib.GrantGroupPermission("grandchildgroup", "complex.test3", plugin);

        //    // 5. Add a user to the lowest group
        //    permLib.AddUserGroup("complexuser", "grandchildgroup");

        //    // 6. Verify permission inheritance works through the chain
        //    Assert.True(permLib.UserHasPermission("complexuser", "complex.test1"));
        //    Assert.True(permLib.UserHasPermission("complexuser", "complex.test2"));
        //    Assert.True(permLib.UserHasPermission("complexuser", "complex.test3"));

        //    // 7. Break the chain and verify permissions change
        //    permLib.SetGroupParent("childgroup1", null);

        //    // User should no longer have rootgroup permissions
        //    Assert.False(permLib.UserHasPermission("complexuser", "complex.test1"));
        //    Assert.True(permLib.UserHasPermission("complexuser", "complex.test2"));
        //    Assert.True(permLib.UserHasPermission("complexuser", "complex.test3"));

        //    // 8. Try a circular reference
        //    Assert.False(permLib.SetGroupParent("rootgroup", "grandchildgroup"));

        //    // 9. Remove the test plugin
        //    var method = typeof(Permission).GetMethod("owner_OnRemovedFromManager",
        //        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        //    var pluginManager = new PluginManager();
        //    method.Invoke(permLib, new object[] { plugin, pluginManager });

        //    // 10. Permissions should be unregistered
        //    Assert.False(permLib.PermissionExists("complex.test1"));
        //    Assert.False(permLib.PermissionExists("complex.test2"));
        //    Assert.False(permLib.PermissionExists("complex.test3"));
        //}

        [Fact]
        public void ProtoStorage_SaveAndLoad_EdgeCases()
        {
            // This test covers edge cases in the ProtoStorage methods used by Permission

            // Create groups and users with special characters in names and permissions
            permLib.CreateGroup("group~!@#$%", "Special Character Group", 1);
            permLib.CreateGroup("empty-perms-group", "Empty Perms", 1);

            var plugin = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();
            permLib.RegisterPermission("special.chars.!@#$%^&*()_+", plugin);

            // Grant the permission
            permLib.GrantGroupPermission("group~!@#$%", "special.chars.!@#$%^&*()_+", plugin);

            // Save data
            permLib.SaveData();

            // Create a new instance to load the data
            var newPermLib = new Permission();

            // Verify the special characters were preserved
            Assert.True(newPermLib.GroupExists("group~!@#$%"));
            Assert.True(newPermLib.GroupHasPermission("group~!@#$%", "special.chars.!@#$%^&*()_+"));
        }

        [Fact]
        public void GrantGroupPermission_WithNonExistentPlugin()
        {
            // Create a group
            permLib.CreateGroup("testnonplugingrant", "Test Non-Plugin Grant", 1);

            // Register a permission with our first plugin
            var plugin = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();
            permLib.RegisterPermission("test.permission", plugin);

            // Create another plugin that doesn't register any permissions
            var emptyPlugin = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();

            // Try to grant a permission using the empty plugin (should do nothing)
            permLib.GrantGroupPermission("testnonplugingrant", "test.permission", emptyPlugin);

            // Verify the permission was not granted
            Assert.False(permLib.GroupHasPermission("testnonplugingrant", "test.permission"));

            // Now grant with the correct plugin
            permLib.GrantGroupPermission("testnonplugingrant", "test.permission", plugin);

            // Verify the permission was granted
            Assert.True(permLib.GroupHasPermission("testnonplugingrant", "test.permission"));
        }

        [Fact]
        public void DataCorruptionRecovery_CompleteTest()
        {
            // Create some initial data
            permLib.CreateGroup("recoverygroup", "Recovery Group", 1);
            var plugin = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();
            permLib.RegisterPermission("recovery.test", plugin);
            permLib.GrantGroupPermission("recoverygroup", "recovery.test", plugin);
            permLib.AddUserGroup("recoveryuser", "recoverygroup");

            // Save the data
            permLib.SaveData();

            // Now corrupt the files
            string usersFile = Path.Combine(tempDataDir, "oxide.users.data");
            string groupsFile = Path.Combine(tempDataDir, "oxide.groups.data");

            // Corrupt the files by appending invalid data
            File.AppendAllText(usersFile, "corruption");
            File.AppendAllText(groupsFile, "corruption");

            // Create a new instance which should recover from corruption
            var recoveryPermLib = new Permission();

            // Should have initialized with empty data due to corruption
            Assert.Empty(recoveryPermLib.GetGroups());

            // Create the data again in the recovery instance
            recoveryPermLib.CreateGroup("recoverygroup2", "Recovery Group 2", 1);
            recoveryPermLib.RegisterPermission("recovery.test2", plugin);
            recoveryPermLib.GrantGroupPermission("recoverygroup2", "recovery.test2", plugin);

            // Save the data from the recovery instance
            recoveryPermLib.SaveData();

            // Create a final instance to verify data was saved correctly
            var finalPermLib = new Permission();

            // Should have the recovered data
            Assert.Contains("recoverygroup2", finalPermLib.GetGroups());
            Assert.True(finalPermLib.GroupHasPermission("recoverygroup2", "recovery.test2"));
        }

        [Fact]
        public void WildcardPermissionEdgeCases()
        {
            // Create test data
            var plugin = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();

            // Register a set of permissions with common prefixes
            permLib.RegisterPermission("wild.card.test1", plugin);
            permLib.RegisterPermission("wild.card.test2", plugin);
            permLib.RegisterPermission("wild.other.test1", plugin);

            // Test wildcard permission granting for users
            permLib.GrantUserPermission("wildcarduser", "wild.card.*", plugin);

            // Should have all permissions matching the wildcard
            Assert.True(permLib.UserHasPermission("wildcarduser", "wild.card.test1"));
            Assert.True(permLib.UserHasPermission("wildcarduser", "wild.card.test2"));
            Assert.False(permLib.UserHasPermission("wildcarduser", "wild.other.test1"));

            // Test wildcard permission revoking for users
            permLib.RevokeUserPermission("wildcarduser", "wild.card.*");

            // Should have no permissions after revoke
            Assert.False(permLib.UserHasPermission("wildcarduser", "wild.card.test1"));
            Assert.False(permLib.UserHasPermission("wildcarduser", "wild.card.test2"));

            // Test wildcard permission granting for groups
            permLib.CreateGroup("wildcardgroup", "Wildcard Group", 1);
            permLib.GrantGroupPermission("wildcardgroup", "wild.*", plugin);

            // Should have all permissions matching the wildcard
            Assert.True(permLib.GroupHasPermission("wildcardgroup", "wild.card.test1"));
            Assert.True(permLib.GroupHasPermission("wildcardgroup", "wild.card.test2"));
            Assert.True(permLib.GroupHasPermission("wildcardgroup", "wild.other.test1"));

            // Test wildcard permission revoking for groups
            permLib.RevokeGroupPermission("wildcardgroup", "wild.*");

            // Should have no permissions after revoke
            Assert.False(permLib.GroupHasPermission("wildcardgroup", "wild.card.test1"));
            Assert.False(permLib.GroupHasPermission("wildcardgroup", "wild.card.test2"));
            Assert.False(permLib.GroupHasPermission("wildcardgroup", "wild.other.test1"));
        }

        [Fact]
        public void ServerConsole_PermissionCheck()
        {
            // Server console should always have all permissions
            Assert.True(permLib.UserHasPermission("server_console", "any.permission.at.all"));
            Assert.True(permLib.UserHasPermission("server_console", "does.not.exist"));

            // Even with empty permissions
            Assert.True(permLib.UserHasPermission("server_console", ""));
        }

        public void Dispose()
        {
            var oxide = Interface.Oxide;
            var instanceDirProp = oxide.GetType().GetProperty("InstanceDirectory", BindingFlags.Public | BindingFlags.Instance);
            if (instanceDirProp != null && originalInstanceDir != null)
            {
                instanceDirProp.SetValue(oxide, originalInstanceDir);
            }
            if (Directory.Exists(tempInstanceDir))
            {
                Directory.Delete(tempInstanceDir, true);
            }
        }

        [Fact]
        public void VerifyGroupData_DuplicateGroups_MergesPermissions()
        {
            Dictionary<string, GroupData> testData = new Dictionary<string, GroupData>(StringComparer.OrdinalIgnoreCase);
            
            // Create first group
            var group1 = new GroupData();
            group1.Perms.Add("test.perm1");
            group1.Perms.Add("test.perm2");
            testData.Add("testgroup", group1);
            
            // Create duplicate group with different casing and different permissions
            var group2 = new GroupData();
            group2.Perms.Add("test.perm3");
            group2.Perms.Add("test.perm4");
            testData.Add("TestGroup", group2);
            
            // Use reflection to access the private method
            var method = typeof(Permission).GetMethod("VerifyGroupData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (Dictionary<string, GroupData>)method.Invoke(permLib, new object[] { testData });
            
            // Verify results
            Assert.Single(result); // Should only have one group after merge
            Assert.True(result.ContainsKey("testgroup"));
            
            // Should have all permissions merged
            var mergedGroup = result["testgroup"];
            Assert.Equal(4, mergedGroup.Perms.Count);
            Assert.True(mergedGroup.Perms.Contains("test.perm1"));
            Assert.True(mergedGroup.Perms.Contains("test.perm2"));
            Assert.True(mergedGroup.Perms.Contains("test.perm3"));
            Assert.True(mergedGroup.Perms.Contains("test.perm4"));
        }

        [Fact]
        public void GrantUserPermission_WithWildcard_GrantsAllPermissions()
        {
            var plugin = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();
            permLib.RegisterPermission("test.perm1", plugin);
            permLib.RegisterPermission("test.perm2", plugin);
            permLib.RegisterPermission("test.perm3", plugin);
            permLib.RegisterPermission("other.perm1", plugin);
            
            // Grant using wildcard (*)
            permLib.GrantUserPermission("testuser", "*", plugin);
            
            // Verify all permissions are granted
            var userPerms = permLib.GetUserPermissions("testuser");
            Assert.Contains("test.perm1", userPerms);
            Assert.Contains("test.perm2", userPerms);
            Assert.Contains("test.perm3", userPerms);
            Assert.Contains("other.perm1", userPerms);
        }

        [Fact]
        public void GrantUserPermission_WithSpecificWildcard_GrantsMatchingPermissions()
        {
            var plugin = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();
            permLib.RegisterPermission("test.perm1", plugin);
            permLib.RegisterPermission("test.perm2", plugin);
            permLib.RegisterPermission("test.other.perm", plugin);
            permLib.RegisterPermission("other.perm1", plugin);
            
            // Grant using specific prefix wildcard
            permLib.GrantUserPermission("testuser", "test.*", plugin);
            
            // Verify only matching permissions are granted
            var userPerms = permLib.GetUserPermissions("testuser");
            Assert.Contains("test.perm1", userPerms);
            Assert.Contains("test.perm2", userPerms);
            Assert.Contains("test.other.perm", userPerms);
            Assert.DoesNotContain("other.perm1", userPerms);
        }

        [Fact]
        public void GrantUserPermission_WithWildcard_NoPluginOwner()
        {
            var plugin1 = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();
            var plugin2 = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();
            plugin2.Name = "FakePlugin2";
            
            permLib.RegisterPermission("plugin1.perm1", plugin1);
            permLib.RegisterPermission("plugin1.perm2", plugin1);
            permLib.RegisterPermission("plugin2.perm1", plugin2);
            
            // Grant using wildcard with null plugin owner
            permLib.GrantUserPermission("testuser", "*", null);
            
            // Verify permissions from all plugins are granted
            var userPerms = permLib.GetUserPermissions("testuser");
            Assert.Contains("plugin1.perm1", userPerms);
            Assert.Contains("plugin1.perm2", userPerms);
            Assert.Contains("plugin2.perm1", userPerms);
        }

        // [Fact]
        // public void GrantUserPermission_AlreadyGranted_DoesNotCallHook()
        // {
        //     var plugin = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();
        //     permLib.RegisterPermission("test.perm", plugin);
            
        //     // Grant the permission first time
        //     permLib.GrantUserPermission("testuser", "test.perm", plugin);
            
        //     // Now grant again - should not add or call hook
        //     bool hookCalled = false;
        //     Interface.Oxide.OnCallHook += (hook, args) => {
        //         if (hook == "OnUserPermissionGranted" && (string)args[0] == "testuser" && (string)args[1] == "test.perm")
        //             hookCalled = true;
        //         return null;
        //     };
            
        //     permLib.GrantUserPermission("testuser", "test.perm", plugin);
            
        //     // Verify hook was not called for duplicate grant
        //     Assert.False(hookCalled);
        // }

        [Fact]
        public void VerifyAndLoadUsersData_MergesDuplicateUsers()
        {
            // Create test user data file with duplicates
            var testUsersData = new Dictionary<string, UserData>(StringComparer.OrdinalIgnoreCase);
            
            // First user entry
            var user1 = new UserData();
            user1.Perms.Add("test.perm1");
            user1.Perms.Add("test.perm2");
            user1.Groups.Add("group1");
            testUsersData.Add("testuser", user1);
            
            // Duplicate user with different perms and groups
            var user2 = new UserData();
            user2.Perms.Add("test.perm3");
            user2.Groups.Add("group2");
            testUsersData.Add("TestUser", user2); // Same name different case
            
            // Save test data to file
            ProtoStorage.Save(testUsersData, "oxide.users");
            
            // Create new permission instance to load the test data
            var tempPermission = new Permission();
            
            // Verify user data was merged
            var userData = tempPermission.GetUserData("testuser");
            Assert.Equal(3, userData.Perms.Count);
            Assert.Contains("test.perm1", userData.Perms);
            Assert.Contains("test.perm2", userData.Perms);
            Assert.Contains("test.perm3", userData.Perms);
            
            Assert.Equal(2, userData.Groups.Count);
            Assert.Contains("group1", userData.Groups);
            Assert.Contains("group2", userData.Groups);
        }

        [Fact]
        public void VerifyAndLoadGroupsData_MergesDuplicateGroups()
        {
            // Create test group data with duplicates
            var testGroupsData = new Dictionary<string, GroupData>(StringComparer.OrdinalIgnoreCase);
            
            // First group entry
            var group1 = new GroupData();
            group1.Title = "Test Group";
            group1.Rank = 1;
            group1.Perms.Add("test.perm1");
            group1.Perms.Add("test.perm2");
            testGroupsData.Add("testgroup", group1);
            
            // Duplicate group with different perms
            var group2 = new GroupData();
            group2.Title = "Test Group 2"; // Different title
            group2.Rank = 2; // Different rank
            group2.Perms.Add("test.perm3");
            testGroupsData.Add("TestGroup", group2); // Same name different case
            
            // Save test data to file
            ProtoStorage.Save(testGroupsData, "oxide.groups");
            
            // Create new permission instance to load the test data
            var tempPermission = new Permission();
            
            // Verify groups data was merged
            var groupData = tempPermission.GetGroupData("testgroup");
            Assert.NotNull(groupData);
            Assert.Equal(3, groupData.Perms.Count);
            Assert.Contains("test.perm1", groupData.Perms);
            Assert.Contains("test.perm2", groupData.Perms);
            Assert.Contains("test.perm3", groupData.Perms);
        }

        [Fact]
        public void GrantGroupPermission_WithWildcard_GrantsAllPermissions()
        {
            var plugin = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();
            permLib.RegisterPermission("test.perm1", plugin);
            permLib.RegisterPermission("test.perm2", plugin);
            permLib.RegisterPermission("other.perm", plugin);
            
            permLib.CreateGroup("testgroup", "Test Group", 1);
            
            // Grant using wildcard
            permLib.GrantGroupPermission("testgroup", "*", plugin);
            
            // Verify all permissions are granted
            var groupPerms = permLib.GetGroupPermissions("testgroup");
            Assert.Contains("test.perm1", groupPerms);
            Assert.Contains("test.perm2", groupPerms);
            Assert.Contains("other.perm", groupPerms);
        }

        [Fact]
        public void GrantGroupPermission_WithNullPlugin_GrantsPermissionsFromAllPlugins()
        {
            var plugin1 = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();
            var plugin2 = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();
            plugin2.Name = "FakePlugin2";
            
            permLib.RegisterPermission("plugin1.perm", plugin1);
            permLib.RegisterPermission("plugin2.perm", plugin2);
            
            permLib.CreateGroup("testgroup", "Test Group", 1);
            
            // Grant using wildcard with null plugin
            permLib.GrantGroupPermission("testgroup", "*", null);
            
            // Verify permissions from all plugins are granted
            var groupPerms = permLib.GetGroupPermissions("testgroup");
            Assert.Contains("plugin1.perm", groupPerms);
            Assert.Contains("plugin2.perm", groupPerms);
        }

        [Fact]
        public void ProtoStorage_SaveAndLoad_WithCorruptedData()
        {
            // Create valid test data
            var testUsersData = new Dictionary<string, UserData>(StringComparer.OrdinalIgnoreCase);
            var user = new UserData();
            user.Perms.Add("test.perm");
            testUsersData.Add("testuser", user);
            
            // Save valid data first
            ProtoStorage.Save(testUsersData, "oxide.users");
            
            // Now corrupt the file by writing invalid data
            string filePath = Path.Combine(Interface.Oxide.InstanceDirectory, "oxide.users.data");
            File.WriteAllText(filePath, "This is not valid protobuf data");
            
            // Try to load the corrupted data
            var loadedData = ProtoStorage.Load<Dictionary<string, UserData>>("oxide.users");
            
            // Should handle gracefully and return null or empty
            Assert.Null(loadedData);
            
            // Create new permission instance which should handle the corrupted data
            var tempPermission = new Permission();
            
            // Should have created a new empty dictionary
            Assert.NotNull(tempPermission.GetUserData("newuser"));
        }

        [Fact]
        public void PermissionExists_ComplexWildcardScenarios()
        {
            var plugin = new Oxide.Core.Tests.Plugins.Mocks.FakePlugin();
            
            // Register some permissions with common prefixes
            permLib.RegisterPermission("test.permission.one", plugin);
            permLib.RegisterPermission("test.permission.two", plugin);
            permLib.RegisterPermission("test.other.permission", plugin);
            permLib.RegisterPermission("different.permission", plugin);
            
            // Test exact match
            Assert.True(permLib.PermissionExists("test.permission.one"));
            
            // Test wildcard at end
            Assert.True(permLib.PermissionExists("test.permission.*"));
            Assert.True(permLib.PermissionExists("test.*"));
            Assert.True(permLib.PermissionExists("*"));
            
            // Test wildcard with specific plugin
            Assert.True(permLib.PermissionExists("test.permission.*", plugin));
            Assert.False(permLib.PermissionExists("nonexistent.*", plugin));
            
            // Test with empty string
            Assert.False(permLib.PermissionExists(""));
            Assert.False(permLib.PermissionExists("", plugin));
        }

        [Fact]
        public void RevokeGroupPermission_AllEdgeCases()
        {
            // Setup a test group with permissions
            permLib.CreateGroup("testGroup", "Test Group", 0);
            permLib.GrantGroupPermission("testGroup", "test.permission1", null);
            permLib.GrantGroupPermission("testGroup", "test.permission2", null);
            permLib.GrantGroupPermission("testGroup", "other.permission", null);

            // Test with non-existing group
            permLib.RevokeGroupPermission("nonExistingGroup", "test.permission1");

            // Test with empty permission
            permLib.RevokeGroupPermission("testGroup", "");
            
            // Test with null permission
            permLib.RevokeGroupPermission("testGroup", null);

            // Test revoking a permission that doesn't exist in the group
            permLib.RevokeGroupPermission("testGroup", "nonexistent.permission");
            
            // Test with wildcard (*) on empty group
            var emptyGroup = "emptyGroup";
            permLib.CreateGroup(emptyGroup, "Empty Group", 0);
            permLib.RevokeGroupPermission(emptyGroup, "*");
            
            // Test removing permissions with wildcard pattern
            permLib.RevokeGroupPermission("testGroup", "test.*");
            
            // Verify the non-matching permission remains
            var permissions = permLib.GetGroupPermissions("testGroup");
            Assert.Single(permissions);
            Assert.Contains("other.permission", permissions);
            
            // Test removing all remaining permissions
            permLib.RevokeGroupPermission("testGroup", "*");
            
            // Verify all permissions are removed
            Assert.Empty(permLib.GetGroupPermissions("testGroup"));
        }

        [Fact]
        public void HasCircularParent_AdditionalEdgeCases()
        {
            // Setup a more complex group hierarchy
            permLib.CreateGroup("a", "Group A", 1);
            permLib.CreateGroup("b", "Group B", 2);
            permLib.CreateGroup("c", "Group C", 3);
            permLib.CreateGroup("d", "Group D", 4);
            permLib.CreateGroup("e", "Group E", 5);
            permLib.CreateGroup("f", "Group F", 6);
            
            // Set up some parent relationships
            permLib.SetGroupParent("b", "a");
            permLib.SetGroupParent("c", "b");
            permLib.SetGroupParent("d", "c");
            
            // Get HasCircularParent method through reflection
            var methodInfo = permLib.GetType().GetMethod("HasCircularParent",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            // Test with non-existent parent group
            var result1 = (bool)methodInfo.Invoke(permLib, new object[] { "e", "nonexistent" });
            Assert.False(result1);
            
            // Test with parent being the same as child (direct circular reference)
            var result2 = (bool)methodInfo.Invoke(permLib, new object[] { "f", "f" });
            Assert.True(result2);
            
            // Create a nested circular reference: e -> d -> c -> b -> a -> e
            permLib.SetGroupParent("e", "d");
            permLib.SetGroupParent("a", "e");
            
            // Check for circular reference from each point in the chain
            var result3 = (bool)methodInfo.Invoke(permLib, new object[] { "a", "b" });
            Assert.True(result3);
            
            var result4 = (bool)methodInfo.Invoke(permLib, new object[] { "b", "c" });
            Assert.True(result4);
            
            var result5 = (bool)methodInfo.Invoke(permLib, new object[] { "c", "d" });
            Assert.True(result5);
            
            var result6 = (bool)methodInfo.Invoke(permLib, new object[] { "d", "e" });
            Assert.True(result6);
            
            var result7 = (bool)methodInfo.Invoke(permLib, new object[] { "e", "a" });
            Assert.True(result7);
            
            // Create an isolated group with no parent
            permLib.CreateGroup("isolated", "Isolated", 10);
            
            // Test isolated group with no parent
            var result8 = (bool)methodInfo.Invoke(permLib, new object[] { "isolated", "nonexistent" });
            Assert.False(result8);
        }

        [Fact]
        public void GroupHasPermission_CompleteEdgeCases()
        {
            // Test with null or empty group name and permission
            Assert.False(permLib.GroupHasPermission(null, "test.permission"));
            Assert.False(permLib.GroupHasPermission("", "test.permission"));
            Assert.False(permLib.GroupHasPermission("someGroup", null));
            Assert.False(permLib.GroupHasPermission("someGroup", ""));
            
            // Test with non-existent group
            Assert.False(permLib.GroupHasPermission("nonExistentGroup", "test.permission"));
            
            // Setup parent-child group hierarchy with permissions
            permLib.CreateGroup("parentGroup", "Parent Group", 1);
            permLib.CreateGroup("childGroup", "Child Group", 2);
            permLib.SetGroupParent("childGroup", "parentGroup");
            
            // Grant permission to parent only
            permLib.GrantGroupPermission("parentGroup", "parent.permission", null);
            
            // Child group should inherit parent's permission
            Assert.True(permLib.GroupHasPermission("childGroup", "parent.permission"));
            
            // Parent should not have child's permissions
            permLib.GrantGroupPermission("childGroup", "child.permission", null);
            Assert.False(permLib.GroupHasPermission("parentGroup", "child.permission"));
            
            // Create a broken parent reference (parent doesn't exist)
            permLib.CreateGroup("brokenParentGroup", "Broken Parent Group", 3);
            
            // Use reflection to set an invalid parent reference
            var groupData = permLib.GetGroupData("brokenParentGroup");
            var parentGroupField = groupData.GetType().GetProperty("ParentGroup");
            parentGroupField.SetValue(groupData, "nonExistentParent");
            
            // Should not throw exception and return false
            Assert.False(permLib.GroupHasPermission("brokenParentGroup", "anything"));
            
            // Test with a multi-level hierarchy
            permLib.CreateGroup("grandparentGroup", "Grandparent Group", 4);
            permLib.CreateGroup("parentGroup2", "Parent Group 2", 5);
            permLib.CreateGroup("childGroup2", "Child Group 2", 6);
            
            permLib.SetGroupParent("parentGroup2", "grandparentGroup");
            permLib.SetGroupParent("childGroup2", "parentGroup2");
            
            // Grant permission only to grandparent
            permLib.GrantGroupPermission("grandparentGroup", "inheritance.test", null);
            
            // Permission should be inherited through multiple levels
            Assert.True(permLib.GroupHasPermission("childGroup2", "inheritance.test"));
            Assert.True(permLib.GroupHasPermission("parentGroup2", "inheritance.test"));
        }

        [Fact]
        public void GroupTitleAndRank_CompleteEdgeCases()
        {
            // Test with null, empty, or non-existent group
            Assert.Empty(permLib.GetGroupTitle(null));
            Assert.Empty(permLib.GetGroupTitle(""));
            Assert.Empty(permLib.GetGroupTitle("nonExistentGroup"));
            
            Assert.Equal(0, permLib.GetGroupRank(null));
            Assert.Equal(0, permLib.GetGroupRank(""));
            Assert.Equal(0, permLib.GetGroupRank("nonExistentGroup"));
            
            Assert.False(permLib.SetGroupTitle(null, "Any Title"));
            Assert.False(permLib.SetGroupTitle("", "Any Title"));
            Assert.False(permLib.SetGroupTitle("nonExistentGroup", "Any Title"));
            
            Assert.False(permLib.SetGroupRank(null, 100));
            Assert.False(permLib.SetGroupRank("", 100));
            Assert.False(permLib.SetGroupRank("nonExistentGroup", 100));
            
            // Create a test group and verify normal operations
            permLib.CreateGroup("testGroup", "Original Title", 5);
            
            // Verify initial values
            Assert.Equal("Original Title", permLib.GetGroupTitle("testGroup"));
            Assert.Equal(5, permLib.GetGroupRank("testGroup"));
            
            // Update title and verify
            Assert.True(permLib.SetGroupTitle("testGroup", "New Title"));
            Assert.Equal("New Title", permLib.GetGroupTitle("testGroup"));
            
            // Update title to null or empty and verify
            Assert.True(permLib.SetGroupTitle("testGroup", null));
            Assert.Equal("", permLib.GetGroupTitle("testGroup"));
            
            Assert.True(permLib.SetGroupTitle("testGroup", ""));
            Assert.Equal("", permLib.GetGroupTitle("testGroup"));
            
            // Reset to a valid title
            permLib.SetGroupTitle("testGroup", "Reset Title");
            
            // Update rank and verify
            Assert.True(permLib.SetGroupRank("testGroup", 10));
            Assert.Equal(10, permLib.GetGroupRank("testGroup"));
            
            // Update rank to negative value and verify it's accepted
            Assert.True(permLib.SetGroupRank("testGroup", -5));
            Assert.Equal(-5, permLib.GetGroupRank("testGroup"));
            
            // Test case insensitivity
            Assert.Equal("Reset Title", permLib.GetGroupTitle("TESTGROUP"));
            Assert.Equal(-5, permLib.GetGroupRank("testgroup"));
            Assert.True(permLib.SetGroupTitle("TestGroup", "Case Insensitive"));
            Assert.True(permLib.SetGroupRank("TESTgroup", 42));
            Assert.Equal("Case Insensitive", permLib.GetGroupTitle("testGROUP"));
            Assert.Equal(42, permLib.GetGroupRank("testgroup"));
        }

        [Fact]
        public void GrantPermission_AdditionalEdgeCases()
        {
            // Create test plugin
            var mockPlugin = new FakePlugin();
            
            // Register test permissions
            permLib.RegisterPermission("test.permission1", mockPlugin);
            permLib.RegisterPermission("test.permission2", mockPlugin);
            permLib.RegisterPermission("test.sub.permission1", mockPlugin);
            permLib.RegisterPermission("test.sub.permission2", mockPlugin);
            permLib.RegisterPermission("other.permission", mockPlugin);
            
            // User permission edge cases
            
            // Test with null or empty user/permission
            permLib.GrantUserPermission(null, "test.permission1", mockPlugin);
            permLib.GrantUserPermission("", "test.permission1", mockPlugin);
            permLib.GrantUserPermission("testUser", null, mockPlugin);
            permLib.GrantUserPermission("testUser", "", mockPlugin);
            
            // Test with unregistered permission
            permLib.GrantUserPermission("testUser", "unregistered.permission", mockPlugin);
            var userPerms = permLib.GetUserPermissions("testUser");
            Assert.DoesNotContain("unregistered.permission", userPerms);
            
            // Test with wildcard permission but no matching registered permissions
            permLib.GrantUserPermission("testUser", "nomatch.*", mockPlugin);
            userPerms = permLib.GetUserPermissions("testUser");
            Assert.DoesNotContain("nomatch.*", userPerms);
            
            // Test with subwildcard permission pattern
            permLib.GrantUserPermission("testUser", "test.sub.*", mockPlugin);
            userPerms = permLib.GetUserPermissions("testUser");
            Assert.Contains("test.sub.permission1", userPerms);
            Assert.Contains("test.sub.permission2", userPerms);
            Assert.DoesNotContain("test.permission1", userPerms);
            
            // Group permission edge cases
            
            // Create test group
            permLib.CreateGroup("testGroupPerms", "Test Group", 0);
            
            // Test with null or empty group/permission
            permLib.GrantGroupPermission(null, "test.permission1", mockPlugin);
            permLib.GrantGroupPermission("", "test.permission1", mockPlugin);
            permLib.GrantGroupPermission("testGroupPerms", null, mockPlugin);
            permLib.GrantGroupPermission("testGroupPerms", "", mockPlugin);
            
            // Test with non-existent group
            permLib.GrantGroupPermission("nonExistentGroup", "test.permission1", mockPlugin);
            
            // Test with unregistered permission
            permLib.GrantGroupPermission("testGroupPerms", "unregistered.permission", mockPlugin);
            var groupPerms = permLib.GetGroupPermissions("testGroupPerms");
            Assert.DoesNotContain("unregistered.permission", groupPerms);
            
            // Test with wildcard permission but no matching registered permissions
            permLib.GrantGroupPermission("testGroupPerms", "nomatch.*", mockPlugin);
            groupPerms = permLib.GetGroupPermissions("testGroupPerms");
            Assert.DoesNotContain("nomatch.*", groupPerms);
            
            // Test with subwildcard permission pattern
            permLib.GrantGroupPermission("testGroupPerms", "test.sub.*", mockPlugin);
            groupPerms = permLib.GetGroupPermissions("testGroupPerms");
            Assert.Contains("test.sub.permission1", groupPerms);
            Assert.Contains("test.sub.permission2", groupPerms);
            Assert.DoesNotContain("test.permission1", groupPerms);
            
            // Test with null plugin
            var anotherPlugin = new FakePlugin();
            permLib.RegisterPermission("plugin.specific", anotherPlugin);
            permLib.GrantUserPermission("testUser", "plugin.specific", null);
            userPerms = permLib.GetUserPermissions("testUser");
            Assert.Contains("plugin.specific", userPerms);
            
            permLib.GrantGroupPermission("testGroupPerms", "plugin.specific", null);
            groupPerms = permLib.GetGroupPermissions("testGroupPerms");
            Assert.Contains("plugin.specific", groupPerms);
        }

        [Fact]
        public void RevokeUserPermission_ExtendedEdgeCases()
        {
            // Create test plugin
            var mockPlugin = new FakePlugin();
            
            // Register test permissions
            permLib.RegisterPermission("test.permission1", mockPlugin);
            permLib.RegisterPermission("test.permission2", mockPlugin);
            permLib.RegisterPermission("test.sub.permission1", mockPlugin);
            permLib.RegisterPermission("test.sub.permission2", mockPlugin);
            permLib.RegisterPermission("other.permission", mockPlugin);
            
            // Set up a user with permissions
            var testUser = "revokeTestUser";
            permLib.GrantUserPermission(testUser, "test.permission1", mockPlugin);
            permLib.GrantUserPermission(testUser, "test.permission2", mockPlugin);
            permLib.GrantUserPermission(testUser, "test.sub.permission1", mockPlugin);
            permLib.GrantUserPermission(testUser, "test.sub.permission2", mockPlugin);
            permLib.GrantUserPermission(testUser, "other.permission", mockPlugin);
            
            // Test with null/empty values
            permLib.RevokeUserPermission(null, "test.permission1");
            permLib.RevokeUserPermission("", "test.permission1");
            permLib.RevokeUserPermission(testUser, null);
            permLib.RevokeUserPermission(testUser, "");
            
            // Verify permissions are still intact
            var userPerms = permLib.GetUserPermissions(testUser);
            Assert.Contains("test.permission1", userPerms);
            Assert.Contains("test.permission2", userPerms);
            Assert.Contains("test.sub.permission1", userPerms);
            Assert.Contains("test.sub.permission2", userPerms);
            Assert.Contains("other.permission", userPerms);
            
            // Test with non-existent user
            permLib.RevokeUserPermission("nonExistentUser", "test.permission1");
            
            // Test revoking a permission the user doesn't have
            permLib.RevokeUserPermission(testUser, "nonexistent.permission");
            
            // Test revoking with wildcard but no matching permissions
            permLib.RevokeUserPermission(testUser, "nomatch.*");
            
            // Verify permissions are still intact
            userPerms = permLib.GetUserPermissions(testUser);
            Assert.Contains("test.permission1", userPerms);
            Assert.Contains("test.permission2", userPerms);
            
            // Test revoking with subwildcard
            permLib.RevokeUserPermission(testUser, "test.sub.*");
            
            // Verify only matching permissions are removed
            userPerms = permLib.GetUserPermissions(testUser);
            Assert.Contains("test.permission1", userPerms);
            Assert.Contains("test.permission2", userPerms);
            Assert.DoesNotContain("test.sub.permission1", userPerms);
            Assert.DoesNotContain("test.sub.permission2", userPerms);
            Assert.Contains("other.permission", userPerms);
            
            // Test revoking with global wildcard
            permLib.RevokeUserPermission(testUser, "*");
            
            // Verify all permissions are removed
            userPerms = permLib.GetUserPermissions(testUser);
            Assert.Empty(userPerms);
        }
        
        [Fact]
        public void SetGroupParent_ExtendedEdgeCases()
        {
            permLib.CreateGroup("parentGroup", "Parent Group", 1);
            permLib.CreateGroup("childGroup", "Child Group", 2);
            permLib.CreateGroup("anotherGroup", "Another Group", 3);
            
            Assert.False(permLib.SetGroupParent(null, "parentGroup"));
            Assert.False(permLib.SetGroupParent("", "parentGroup"));
            Assert.False(permLib.SetGroupParent("childGroup", null));
            
            Assert.False(permLib.SetGroupParent("nonExistentGroup", "parentGroup"));
            
            Assert.False(permLib.SetGroupParent("childGroup", "nonExistentParent"));
            
            Assert.False(permLib.SetGroupParent("parentGroup", "parentGroup"));
            
            Assert.True(permLib.SetGroupParent("childGroup", "parentGroup"));
            Assert.Equal("parentGroup", permLib.GetGroupParent("childGroup"));
            
            Assert.True(permLib.SetGroupParent("childGroup", ""));
            Assert.Empty(permLib.GetGroupParent("childGroup"));
            
            Assert.True(permLib.SetGroupParent("childGroup", "parentGroup"));
            Assert.True(permLib.SetGroupParent("anotherGroup", "childGroup"));
            
            Assert.False(permLib.SetGroupParent("parentGroup", "anotherGroup"));
            
            Assert.Empty(permLib.GetGroupParent("parentGroup"));
            
            Assert.True(permLib.SetGroupParent("CHILDGROUP", "PARENTGROUP"));
            Assert.Equal("parentgroup", permLib.GetGroupParent("childGroup").ToLower());
        }

        [Fact]
        public void GetGroupPermissions_ExhaustiveTests()
        {
            var mockPlugin = new FakePlugin();
            
            permLib.RegisterPermission("test.permission1", mockPlugin);
            permLib.RegisterPermission("test.permission2", mockPlugin);
            permLib.RegisterPermission("parent.permission", mockPlugin);
            permLib.RegisterPermission("child.permission", mockPlugin);
            
            Assert.Empty(permLib.GetGroupPermissions(null));
            Assert.Empty(permLib.GetGroupPermissions(""));
            Assert.Empty(permLib.GetGroupPermissions("nonExistentGroup"));
            
            permLib.CreateGroup("parentGroup", "Parent Group", 1);
            permLib.CreateGroup("childGroup", "Child Group", 2);
            
            Assert.Empty(permLib.GetGroupPermissions("parentGroup"));
            Assert.Empty(permLib.GetGroupPermissions("childGroup"));
            
            permLib.GrantGroupPermission("parentGroup", "parent.permission", mockPlugin);
            permLib.GrantGroupPermission("parentGroup", "test.permission1", mockPlugin);
            
            var parentPerms = permLib.GetGroupPermissions("parentGroup");
            Assert.Equal(2, parentPerms.Length);
            Assert.Contains("parent.permission", parentPerms);
            Assert.Contains("test.permission1", parentPerms);
            
            permLib.SetGroupParent("childGroup", "parentGroup");
            
            permLib.GrantGroupPermission("childGroup", "child.permission", mockPlugin);
            permLib.GrantGroupPermission("childGroup", "test.permission2", mockPlugin);
            
            var childPerms = permLib.GetGroupPermissions("childGroup", false);
            Assert.Equal(2, childPerms.Length);
            Assert.Contains("child.permission", childPerms);
            Assert.Contains("test.permission2", childPerms);
            Assert.DoesNotContain("parent.permission", childPerms);
            Assert.DoesNotContain("test.permission1", childPerms);
            
            var childWithParentPerms = permLib.GetGroupPermissions("childGroup", true);
            Assert.Equal(4, childWithParentPerms.Length);
            Assert.Contains("child.permission", childWithParentPerms);
            Assert.Contains("test.permission2", childWithParentPerms);
            Assert.Contains("parent.permission", childWithParentPerms);
            Assert.Contains("test.permission1", childWithParentPerms);
            
            permLib.CreateGroup("brokenParentGroup", "Broken Parent Group", 3);
            
            var groupData = permLib.GetGroupData("brokenParentGroup");
            var parentGroupField = groupData.GetType().GetProperty("ParentGroup");
            parentGroupField.SetValue(groupData, "nonExistentParent");
            
            permLib.GrantGroupPermission("brokenParentGroup", "test.permission1", mockPlugin);
            
            var brokenParentPerms = permLib.GetGroupPermissions("brokenParentGroup", true);
            Assert.Single(brokenParentPerms);
            Assert.Contains("test.permission1", brokenParentPerms);
            
            permLib.CreateGroup("grandparentGroup", "Grandparent Group", 4);
            permLib.GrantGroupPermission("grandparentGroup", "grandparent.permission", mockPlugin);
            
            permLib.SetGroupParent("parentGroup", "grandparentGroup");
            
            var multiLevelPerms = permLib.GetGroupPermissions("childGroup", true);
            Assert.Equal(5, multiLevelPerms.Length);
            Assert.Contains("child.permission", multiLevelPerms);
            Assert.Contains("test.permission2", multiLevelPerms);
            Assert.Contains("parent.permission", multiLevelPerms);
            Assert.Contains("test.permission1", multiLevelPerms);
            Assert.Contains("grandparent.permission", multiLevelPerms);
        }
    }
}
