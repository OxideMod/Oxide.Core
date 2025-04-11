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
    /// Tests for user permission management in the Permission library.
    /// </summary>
    public class UserPermissionTests : IDisposable
    {
        private readonly Permission permLib;
        private readonly string tempDataDir;
        private readonly FakePlugin testPlugin;
        private readonly string tempInstanceDir;

        /// <summary>
        /// Sets up the test environment with a fresh test directory and Permission instance.
        /// </summary>
        public UserPermissionTests()
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

        #region User Permission Tests

        [Fact]
        public void GrantUserPermission_GrantsPermissionSuccessfully()
        {
            string userId = "userPermGrant";
            string permission = $"{testPlugin.Name}.test";
            permLib.RegisterPermission(permission, testPlugin);
            
            permLib.GrantUserPermission(userId, permission, testPlugin);
            
            var userData = permLib.GetUserData(userId);
            Assert.Contains(permission, userData.Perms);
        }

        [Fact]
        public void GrantUserPermission_FailsForUnregisteredPermission()
        {
            string userId = "userUnregPerm";
            string permission = $"{testPlugin.Name}.unregistered";
            
            // Since we can't directly check the return value, we'll check if the permission
            // is actually added to the user's permissions
            permLib.GrantUserPermission(userId, permission, testPlugin);
            var userData = permLib.GetUserData(userId);
            Assert.DoesNotContain(permission, userData.Perms);
        }

        [Fact]
        public void RevokeUserPermission_RevokesPermissionSuccessfully()
        {
            string userId = "userPermRevoke";
            string permission = $"{testPlugin.Name}.revoke";
            permLib.RegisterPermission(permission, testPlugin);
            permLib.GrantUserPermission(userId, permission, testPlugin);
            
            permLib.RevokeUserPermission(userId, permission);
            
            var userData = permLib.GetUserData(userId);
            Assert.DoesNotContain(permission, userData.Perms);
        }

        [Fact]
        public void RevokeUserPermission_WithWildcard_RevokesAllPermissions()
        {
            string userId = "userWildcardPerm";
            string perm1 = $"{testPlugin.Name}.perm1";
            string perm2 = $"{testPlugin.Name}.perm2";
            permLib.RegisterPermission(perm1, testPlugin);
            permLib.RegisterPermission(perm2, testPlugin);
            permLib.GrantUserPermission(userId, perm1, testPlugin);
            permLib.GrantUserPermission(userId, perm2, testPlugin);
            
            permLib.RevokeUserPermission(userId, "*");
            
            var userData = permLib.GetUserData(userId);
            Assert.Empty(userData.Perms);
        }

        [Fact]
        public void UserHasPermission_DirectUserPermission_ReturnsTrue()
        {
            string userId = "userDirectPerm";
            string permission = $"{testPlugin.Name}.direct";
            permLib.RegisterPermission(permission, testPlugin);
            permLib.GrantUserPermission(userId, permission, testPlugin);
            
            Assert.True(permLib.UserHasPermission(userId, permission));
        }

        [Fact]
        public void UserHasPermission_InheritedGroupPermission_ReturnsTrue()
        {
            string userId = "userGroupPerm";
            string permission = $"{testPlugin.Name}.group";
            permLib.RegisterPermission(permission, testPlugin);
            
            permLib.CreateGroup("permGroup", "Permission Group", 1);
            permLib.GrantGroupPermission("permGroup", permission, testPlugin);
            permLib.AddUserGroup(userId, "permGroup");
            
            Assert.True(permLib.UserHasPermission(userId, permission));
        }

        [Fact]
        public void UserHasPermission_ParentGroupPermission_ReturnsTrue()
        {
            string userId = "userParentGroupPerm";
            string permission = $"{testPlugin.Name}.parent";
            permLib.RegisterPermission(permission, testPlugin);
            
            permLib.CreateGroup("parentPerm", "Parent Group", 10);
            permLib.CreateGroup("childPerm", "Child Group", 1);
            permLib.GrantGroupPermission("parentPerm", permission, testPlugin);
            permLib.SetGroupParent("childPerm", "parentPerm");
            
            permLib.AddUserGroup(userId, "childPerm");
            
            Assert.True(permLib.UserHasPermission(userId, permission));
        }

        [Fact]
        public void UserHasPermission_NoPermission_ReturnsFalse()
        {
            string userId = "userNoPerm";
            string permission = $"{testPlugin.Name}.none";
            permLib.RegisterPermission(permission, testPlugin);
            
            Assert.False(permLib.UserHasPermission(userId, permission));
        }

        [Fact]
        public void GetUserPermissions_ReturnsDirectAndInheritedPermissions()
        {
            string directPerm = $"{testPlugin.Name}.direct";
            string groupPerm = $"{testPlugin.Name}.group";
            permLib.RegisterPermission(directPerm, testPlugin);
            permLib.RegisterPermission(groupPerm, testPlugin);
            
            string userId = "userUnion";
            permLib.GrantUserPermission(userId, directPerm, testPlugin);
            
            bool created = permLib.CreateGroup("unionGroup", "Union Group", 5);
            Assert.True(created);
            permLib.GrantGroupPermission("unionGroup", groupPerm, testPlugin);
            permLib.AddUserGroup(userId, "unionGroup");
            
            var perms = permLib.GetUserPermissions(userId);
            Assert.Contains(directPerm, perms);
            Assert.Contains(groupPerm, perms);
        }

        [Fact]
        public void GetPermissionUsers_ReturnsUsersWithPermission()
        {
            string permission = $"{testPlugin.Name}.users";
            permLib.RegisterPermission(permission, testPlugin);
            
            string user1 = "permUser1";
            string user2 = "permUser2";
            permLib.GrantUserPermission(user1, permission, testPlugin);
            permLib.GrantUserPermission(user2, permission, testPlugin);
            
            var users = permLib.GetPermissionUsers(permission);
            Assert.Equal(2, users.Length);
            
            // The format is "userId(nickname)" so we check for contained substrings
            Assert.Contains(users, s => s.Contains(user1));
            Assert.Contains(users, s => s.Contains(user2));
        }

        /// <summary>
        /// Tests that GetPermissionUsers returns an empty array when an empty permission string is provided.
        /// </summary>
        [Fact]
        public void GetPermissionUsers_EmptyPermission_ReturnsEmptyArray()
        {
            var users = permLib.GetPermissionUsers("");
            Assert.Empty(users);
        }

        /// <summary>
        /// Verifies that GrantUserPermission with a wildcard pattern grants all matching registered permissions.
        /// </summary>
        [Fact]
        public void GrantUserPermission_WithWildcardPermission_GrantsAllMatchingPermissions()
        {
            // Setup: Register multiple permissions
            string perm1 = $"{testPlugin.Name}.test.permission1";
            string perm2 = $"{testPlugin.Name}.test.permission2";
            string perm3 = $"{testPlugin.Name}.other.permission";
            permLib.RegisterPermission(perm1, testPlugin);
            permLib.RegisterPermission(perm2, testPlugin);
            permLib.RegisterPermission(perm3, testPlugin);

            // Grant using wildcard
            string userId = "wildcardUser";
            permLib.GrantUserPermission(userId, $"{testPlugin.Name}.test.*", testPlugin);

            // Verify
            var userData = permLib.GetUserData(userId);
            Assert.Contains(perm1, userData.Perms);
            Assert.Contains(perm2, userData.Perms);
            Assert.DoesNotContain(perm3, userData.Perms);
            
            // Grant using full wildcard
            string userId2 = "allWildcardUser";
            permLib.GrantUserPermission(userId2, "*", testPlugin);
            
            // Verify all permissions are granted
            var userData2 = permLib.GetUserData(userId2);
            Assert.Contains(perm1, userData2.Perms);
            Assert.Contains(perm2, userData2.Perms);
            Assert.Contains(perm3, userData2.Perms);
        }
        
        /// <summary>
        /// Tests that GrantUserPermission works with a null owner parameter if the permission is already registered.
        /// </summary>
        [Fact]
        public void GrantUserPermission_WithNullOwner_GrantsPermissionIfRegistered()
        {
            // Setup: Register permissions
            string permission = $"{testPlugin.Name}.nullowner.permission";
            permLib.RegisterPermission(permission, testPlugin);
            
            // Create test user
            string userId = "nullOwnerUser";
            
            // Grant with null owner
            permLib.GrantUserPermission(userId, permission, null);
            
            // Verify permission was granted
            var userData = permLib.GetUserData(userId);
            Assert.Contains(permission, userData.Perms);
            
            // Try with wildcard and null owner
            string userId2 = "nullOwnerWildcardUser";
            permLib.GrantUserPermission(userId2, "*", null);
            
            // Verify all permissions are granted
            var userData2 = permLib.GetUserData(userId2);
            Assert.Contains(permission, userData2.Perms);
        }
        
        /// <summary>
        /// Verifies that GrantUserPermission with a wildcard pattern that doesn't match any permissions grants nothing.
        /// </summary>
        [Fact]
        public void GrantUserPermission_WithWildcardThatDoesntMatch_GrantsNothing()
        {
            // Setup: Register permissions
            string perm1 = $"{testPlugin.Name}.test.permission1";
            permLib.RegisterPermission(perm1, testPlugin);
            
            // Create test user
            string userId = "noMatchUser";
            
            // Grant with non-matching wildcard
            permLib.GrantUserPermission(userId, "nomatch.*", testPlugin);
            
            // Verify no permissions were granted
            var userData = permLib.GetUserData(userId);
            Assert.Empty(userData.Perms);
        }

        #endregion
    }
} 