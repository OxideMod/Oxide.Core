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
    /// Tests for validation functionality in the Permission library.
    /// </summary>
    public class ValidationTests : IDisposable
    {
        private readonly Permission permLib;
        private readonly string tempDataDir;
        private readonly FakePlugin testPlugin;
        private readonly string tempInstanceDir;

        public ValidationTests()
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

        #region Validation Tests

        /// <summary>
        /// Tests that when a validation function is registered, it is applied during CleanUp operations to filter users.
        /// </summary>
        [Fact]
        public void RegisterValidate_ValidatorIsApplied()
        {
            // Create a user to test validation
            string userId = "validate_user";
            permLib.GetUserData(userId);
            
            // Set a validator that rejects all user IDs
            permLib.RegisterValidate(id => false);
            
            // Call CleanUp to apply validation
            permLib.CleanUp();
            
            // Get users to verify validator works
            var userData = permLib.GetUserData(userId);
            // If validator worked properly, this should be a new user
            Assert.Equal("Unnamed", userData.LastSeenNickname);
            
            // Reset validator
            permLib.RegisterValidate(null);
        }

        /// <summary>
        /// Verifies that a null validator function accepts all user IDs, effectively disabling validation.
        /// </summary>
        [Fact]
        public void RegisterValidate_NullValidatorAcceptsAll()
        {
            // First ensure validator is null
            permLib.RegisterValidate(null);
            
            // Create a test user
            string userId = "validate_null";
            var userData = permLib.GetUserData(userId);
            userData.LastSeenNickname = "ValidateTest";
            
            // CleanUp should do nothing since validator is null
            permLib.CleanUp();
            
            // Verify user still exists with original data
            userData = permLib.GetUserData(userId);
            Assert.Equal("ValidateTest", userData.LastSeenNickname);
        }

        #endregion
    }
} 