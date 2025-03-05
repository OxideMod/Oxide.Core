using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Core.Tests.Plugins.Mocks;

namespace Oxide.Core.Tests.Libraries
{
    /// <summary>
    /// Tests for the Permission library.
    /// </summary>
    public class PermissionTests : IDisposable
    {
        private readonly Permission permLib;
        private readonly string tempInstanceDir;
        private readonly string tempDataDir;
        private readonly string originalInstanceDir;

        /// <summary>
        /// Sets up a temporary instance directory and data directory so that file-based operations work in isolation.
        /// </summary>
        public PermissionTests()
        {
            // Get the Oxide instance
            var oxide = Interface.Oxide;
            // Save original instance directory (if any) so that we can restore later
            var instanceDirProp = oxide.GetType().GetProperty("InstanceDirectory", BindingFlags.Public | BindingFlags.Instance);
            originalInstanceDir = instanceDirProp?.GetValue(oxide) as string;

            // Create a unique temporary folder for our test instance
            tempInstanceDir = Path.Combine(Path.GetTempPath(), "OxidePermissionTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempInstanceDir);

            // Override InstanceDirectory via reflection
            if (instanceDirProp != null)
            {
                instanceDirProp.SetValue(oxide, tempInstanceDir);
            }

            // Set other folders relative to InstanceDirectory (assuming OxideMod.Load uses these paths)
            // Here we assume DataDirectory is at "data" under InstanceDirectory.
            tempDataDir = Path.Combine(tempInstanceDir, "data");
            Directory.CreateDirectory(tempDataDir);

            // Similarly, ensure LangDirectory and ConfigDirectory exist
            string tempLangDir = Path.Combine(tempInstanceDir, "lang");
            if (!Directory.Exists(tempLangDir)) Directory.CreateDirectory(tempLangDir);
            string tempConfigDir = Path.Combine(tempInstanceDir, "config");
            if (!Directory.Exists(tempConfigDir)) Directory.CreateDirectory(tempConfigDir);

            // Create empty oxide.users and oxide.groups files in the data folder
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

            // Reinitialize Oxide if needed
            Interface.Initialize();
            Interface.Oxide.Load();

            // Now create the Permission library instance; it will use the new InstanceDirectory and DataDirectory.
            permLib = new Permission();
        }

        /// <summary>
        /// Tests that registering a permission adds it to the library.
        /// </summary>
        [Fact]
        public void RegisterPermission_AddsPermission()
        {
            var plugin = new FakePlugin();
            string permission = $"{plugin.Name}.test";
            permLib.RegisterPermission(permission, plugin);
            bool exists = permLib.PermissionExists(permission, plugin);
            Assert.True(exists);
        }

        /// <summary>
        /// Tests that granting a permission to a user adds it to the user’s data.
        /// </summary>
        [Fact]
        public void GrantUserPermission_AddsPermissionToUser()
        {
            var plugin = new FakePlugin();
            string permission = $"{plugin.Name}.test";
            permLib.RegisterPermission(permission, plugin);
            string userId = "user123";
            permLib.GrantUserPermission(userId, permission, plugin);
            bool hasPermission = permLib.UserHasPermission(userId, permission);
            Assert.True(hasPermission);
        }

        /// <summary>
        /// Tests that revoking a user permission removes it.
        /// </summary>
        [Fact]
        public void RevokeUserPermission_RemovesPermissionFromUser()
        {
            var plugin = new FakePlugin();
            string permission = $"{plugin.Name}.test";
            permLib.RegisterPermission(permission, plugin);
            string userId = "user123";
            permLib.GrantUserPermission(userId, permission, plugin);
            permLib.RevokeUserPermission(userId, permission);
            bool hasPermission = permLib.UserHasPermission(userId, permission);
            Assert.False(hasPermission);
        }

        /// <summary>
        /// Tests that group management (creating, adding user, removing user, deleting group) works correctly.
        /// </summary>
        [Fact]
        public void GroupManagement_WorksCorrectly()
        {
            string groupName = "admin";
            string groupTitle = "Administrators";
            int rank = 10;
            var plugin = new FakePlugin();
            bool created = permLib.CreateGroup(groupName, groupTitle, rank);
            Assert.True(created);
            string title = permLib.GetGroupTitle(groupName);
            Assert.Equal(groupTitle, title);

            string userId = "user123";
            permLib.AddUserGroup(userId, groupName);
            string[] groups = permLib.GetUserGroups(userId);
            Assert.Contains(groupName, groups);

            permLib.RemoveUserGroup(userId, groupName);
            groups = permLib.GetUserGroups(userId);
            Assert.DoesNotContain(groupName, groups);

            bool removed = permLib.RemoveGroup(groupName);
            Assert.True(removed);
        }

        /// <summary>
        /// Cleans up by restoring the original InstanceDirectory and removing temporary files.
        /// </summary>
        public void Dispose()
        {
            // Restore the original instance directory if it was set
            var oxide = Interface.Oxide;
            var instanceDirProp = oxide.GetType().GetProperty("InstanceDirectory", BindingFlags.Public | BindingFlags.Instance);
            if (instanceDirProp != null && originalInstanceDir != null)
            {
                instanceDirProp.SetValue(oxide, originalInstanceDir);
            }

            // Delete temporary instance directory
            if (Directory.Exists(tempInstanceDir))
            {
                Directory.Delete(tempInstanceDir, true);
            }
        }
    }
}
