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
    /// Tests for group permission management in the Permission library.
    /// </summary>
    public class GroupPermissionTests : IDisposable
    {
        private readonly Permission permLib;
        private readonly string tempDataDir;
        private readonly FakePlugin testPlugin;
        private readonly string tempInstanceDir;

        public GroupPermissionTests()
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

        #region Group Permission Tests

        /// <summary>
        /// Tests that GrantGroupPermission successfully grants a registered permission to a group.
        /// </summary>
        [Fact]
        public void GrantGroupPermission_GrantsPermissionSuccessfully()
        {
            string groupName = "groupPermGrant";
            string permission = $"{testPlugin.Name}.test";
            
            permLib.CreateGroup(groupName, "Group Perm Grant", 1);
            permLib.RegisterPermission(permission, testPlugin);
            
            permLib.GrantGroupPermission(groupName, permission, testPlugin);
            
            var groupData = permLib.GetGroupData(groupName);
            Assert.Contains(permission, groupData.Perms);
        }

        /// <summary>
        /// Ensures that GrantGroupPermission silently fails when attempting to grant an unregistered permission.
        /// </summary>
        [Fact]
        public void GrantGroupPermission_FailsForUnregisteredPermission()
        {
            string groupName = "groupUnregPerm";
            string permission = $"{testPlugin.Name}.unregistered";
            
            permLib.CreateGroup(groupName, "Group Unreg Perm", 1);
            
            // Since we can't directly check the return value, we'll check if the permission
            // is actually added to the group's permissions
            permLib.GrantGroupPermission(groupName, permission, testPlugin);
            var groupData = permLib.GetGroupData(groupName);
            Assert.DoesNotContain(permission, groupData.Perms);
        }

        /// <summary>
        /// Tests that RevokeGroupPermission successfully removes a previously granted permission from a group.
        /// </summary>
        [Fact]
        public void RevokeGroupPermission_RevokesPermissionSuccessfully()
        {
            string groupName = "groupPermRevoke";
            string permission = $"{testPlugin.Name}.revoke";
            
            permLib.CreateGroup(groupName, "Group Perm Revoke", 1);
            permLib.RegisterPermission(permission, testPlugin);
            permLib.GrantGroupPermission(groupName, permission, testPlugin);
            
            permLib.RevokeGroupPermission(groupName, permission);
            
            var groupData = permLib.GetGroupData(groupName);
            Assert.DoesNotContain(permission, groupData.Perms);
        }

        /// <summary>
        /// Verifies that RevokeGroupPermission with wildcard ('*') pattern revokes all permissions from a group.
        /// </summary>
        [Fact]
        public void RevokeGroupPermission_WithWildcard_RevokesAllPermissions()
        {
            string groupName = "groupWildcardPerm";
            string perm1 = $"{testPlugin.Name}.perm1";
            string perm2 = $"{testPlugin.Name}.perm2";
            
            permLib.CreateGroup(groupName, "Group Wildcard Perm", 1);
            permLib.RegisterPermission(perm1, testPlugin);
            permLib.RegisterPermission(perm2, testPlugin);
            permLib.GrantGroupPermission(groupName, perm1, testPlugin);
            permLib.GrantGroupPermission(groupName, perm2, testPlugin);
            
            permLib.RevokeGroupPermission(groupName, "*");
            
            var groupData = permLib.GetGroupData(groupName);
            Assert.Empty(groupData.Perms);
        }

        /// <summary>
        /// Tests that GetGroupPermissions returns only direct group permissions when includeParents is false, 
        /// and includes parent group permissions when includeParents is true.
        /// </summary>
        [Fact]
        public void GetGroupPermissions_WithAndWithoutParents()
        {
            string parentPerm = $"{testPlugin.Name}.parent";
            string childPerm = $"{testPlugin.Name}.child";
            
            permLib.RegisterPermission(parentPerm, testPlugin);
            permLib.RegisterPermission(childPerm, testPlugin);
            
            permLib.CreateGroup("parentGroup", "Parent Group", 10);
            permLib.CreateGroup("childGroup", "Child Group", 1);
            
            permLib.GrantGroupPermission("parentGroup", parentPerm, testPlugin);
            permLib.GrantGroupPermission("childGroup", childPerm, testPlugin);
            permLib.SetGroupParent("childGroup", "parentGroup");
            
            var childOnly = permLib.GetGroupPermissions("childGroup", false);
            Assert.Contains(childPerm, childOnly);
            Assert.DoesNotContain(parentPerm, childOnly);
            
            var withParents = permLib.GetGroupPermissions("childGroup", true);
            Assert.Contains(parentPerm, withParents);
            Assert.Contains(childPerm, withParents);
        }

        /// <summary>
        /// Verifies that GetPermissionGroups returns all groups that have been granted a specific permission.
        /// </summary>
        [Fact]
        public void GetPermissionGroups_ReturnsGroupsWithPermission()
        {
            string permission = $"{testPlugin.Name}.groups";
            permLib.RegisterPermission(permission, testPlugin);
            
            permLib.CreateGroup("permGroup1", "Perm Group 1", 1);
            permLib.CreateGroup("permGroup2", "Perm Group 2", 2);
            
            permLib.GrantGroupPermission("permGroup1", permission, testPlugin);
            permLib.GrantGroupPermission("permGroup2", permission, testPlugin);
            
            var groups = permLib.GetPermissionGroups(permission);
            Assert.Equal(2, groups.Length);
            Assert.Contains("permGroup1", groups);
            Assert.Contains("permGroup2", groups);
        }

        /// <summary>
        /// Tests that GetPermissionGroups returns an empty array when an empty permission string is provided.
        /// </summary>
        [Fact]
        public void GetPermissionGroups_EmptyPermission_ReturnsEmptyArray()
        {
            var groups = permLib.GetPermissionGroups("");
            Assert.Empty(groups);
        }

        /// <summary>
        /// Verifies that GrantGroupPermission with a wildcard pattern grants all matching registered permissions to a group.
        /// </summary>
        [Fact]
        public void GrantGroupPermission_WithWildcardPermission_GrantsAllMatchingPermissions()
        {
            // Setup: Register multiple permissions
            string perm1 = $"{testPlugin.Name}.test.groupperm1";
            string perm2 = $"{testPlugin.Name}.test.groupperm2";
            string perm3 = $"{testPlugin.Name}.other.groupperm";
            permLib.RegisterPermission(perm1, testPlugin);
            permLib.RegisterPermission(perm2, testPlugin);
            permLib.RegisterPermission(perm3, testPlugin);

            // Create test group
            string groupName = "wildcardGroup";
            permLib.CreateGroup(groupName, "Wildcard Group", 1);
            
            // Grant using wildcard
            permLib.GrantGroupPermission(groupName, $"{testPlugin.Name}.test.*", testPlugin);

            // Verify
            var groupData = permLib.GetGroupData(groupName);
            Assert.Contains(perm1, groupData.Perms);
            Assert.Contains(perm2, groupData.Perms);
            Assert.DoesNotContain(perm3, groupData.Perms);
            
            // Grant using full wildcard
            string groupName2 = "allWildcardGroup";
            permLib.CreateGroup(groupName2, "All Wildcard Group", 1);
            permLib.GrantGroupPermission(groupName2, "*", testPlugin);
            
            // Verify all permissions are granted
            var groupData2 = permLib.GetGroupData(groupName2);
            Assert.Contains(perm1, groupData2.Perms);
            Assert.Contains(perm2, groupData2.Perms);
            Assert.Contains(perm3, groupData2.Perms);
        }
        
        /// <summary>
        /// Tests that GrantGroupPermission works with a null owner parameter if the permission is already registered.
        /// </summary>
        [Fact]
        public void GrantGroupPermission_WithNullOwner_GrantsPermissionIfRegistered()
        {
            // Setup: Register permissions
            string permission = $"{testPlugin.Name}.nullowner.groupperm";
            permLib.RegisterPermission(permission, testPlugin);
            
            // Create test group
            string groupName = "nullOwnerGroup";
            permLib.CreateGroup(groupName, "Null Owner Group", 1);
            
            // Grant with null owner
            permLib.GrantGroupPermission(groupName, permission, null);
            
            // Verify permission was granted
            var groupData = permLib.GetGroupData(groupName);
            Assert.Contains(permission, groupData.Perms);
            
            // Try with wildcard and null owner
            string groupName2 = "nullOwnerWildGroup";
            permLib.CreateGroup(groupName2, "Null Owner Wildcard Group", 1);
            permLib.GrantGroupPermission(groupName2, "*", null);
            
            // Verify all permissions are granted
            var groupData2 = permLib.GetGroupData(groupName2);
            Assert.Contains(permission, groupData2.Perms);
        }
        
        /// <summary>
        /// Verifies that GrantGroupPermission with a wildcard pattern that doesn't match any permissions grants nothing.
        /// </summary>
        [Fact]
        public void GrantGroupPermission_WithWildcardThatDoesntMatch_GrantsNothing()
        {
            // Setup: Register permissions
            string perm1 = $"{testPlugin.Name}.test.groupperm";
            permLib.RegisterPermission(perm1, testPlugin);
            
            // Create test group
            string groupName = "noMatchGroup";
            permLib.CreateGroup(groupName, "No Match Group", 1);
            
            // Grant with non-matching wildcard
            permLib.GrantGroupPermission(groupName, "nomatch.*", testPlugin);
            
            // Verify no permissions were granted
            var groupData = permLib.GetGroupData(groupName);
            Assert.Empty(groupData.Perms);
        }

        #endregion
    }
} 