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
    /// Tests for user data functionality in the Permission library.
    /// </summary>
    public class UserDataTests : IDisposable
    {
        private readonly Permission permLib;
        private readonly string tempDataDir;
        private readonly FakePlugin testPlugin;
        private readonly string tempInstanceDir;
        private readonly string originalInstanceDir;

        public UserDataTests()
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

        #region User Data Tests

        /// <summary>
        /// Verifies that GetUserData returns a new UserData object with default values when a user doesn't exist.
        /// </summary>
        [Fact]
        public void GetUserData_ReturnsNewUserDataIfNotExist()
        {
            var userData = permLib.GetUserData("newUser");
            Assert.NotNull(userData);
            Assert.Equal("Unnamed", userData.LastSeenNickname);
            Assert.Empty(userData.Perms);
            Assert.Empty(userData.Groups);
        }

        /// <summary>
        /// Tests that UpdateNickname properly updates the nickname of an existing user.
        /// </summary>
        [Fact]
        public void UpdateNickname_UpdatesExistingUserData()
        {
            var userData = permLib.GetUserData("userNick");
            userData.LastSeenNickname = "OldName";
            permLib.UpdateNickname("userNick", "NewName");
            Assert.Equal("NewName", userData.LastSeenNickname);
        }

        /// <summary>
        /// Verifies that UpdateNickname creates a new user data entry when the specified user doesn't exist.
        /// </summary>
        [Fact]
        public void UpdateNickname_CreatesNewUserIfNotExists()
        {
            string userId = "nonExistingUser";
            
            // First, verify the user doesn't exist
            var initialData = permLib.GetUserData(userId);
            initialData.LastSeenNickname = "OldName";
            
            // Then call UpdateNickname
            permLib.UpdateNickname(userId, "NewUser");
            
            // Check if the nickname was updated
            Assert.Equal("NewUser", initialData.LastSeenNickname);
        }

        #endregion
    }
} 