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
        public void UserHasAnyGroup_NoGroups()
        {
            string userId = "noGroupsUser";
            Assert.False(permLib.UserHasAnyGroup(userId));
            permLib.CreateGroup("someGroup", "Some Group", 1);
            permLib.AddUserGroup(userId, "someGroup");
            Assert.True(permLib.UserHasAnyGroup(userId));
        }

        [Fact]
        public void UpdateNickname_NonExistingUser()
        {
            permLib.UpdateNickname("nonExisting", "NewName");
            var userData = permLib.GetUserData("nonExisting");
            Assert.Equal("NewName", userData.LastSeenNickname);
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
    }
}
