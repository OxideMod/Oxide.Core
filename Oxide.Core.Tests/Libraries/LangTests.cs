using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

namespace Oxide.Core.Tests.Libraries
{
    /// <summary>
    /// Tests for the Lang library.
    /// </summary>
    public class LangTests : IDisposable
    {
        private readonly string testDataDir;
        private readonly string testLangDir;
        private readonly Lang langLib;

        public LangTests()
        {
            // Create temp directories for testing
            testDataDir = Path.Combine(Path.GetTempPath(), "oxide_test_data_" + Guid.NewGuid());
            testLangDir = Path.Combine(testDataDir, "lang");
            Directory.CreateDirectory(testDataDir);
            Directory.CreateDirectory(testLangDir);
            
            // Set Interface.Oxide properties
            SetOxideProperty("DataDirectory", testDataDir);
            SetOxideProperty("LangDirectory", testLangDir);
            
            // Create Lang instance
            langLib = new Lang();
        }

        private void SetOxideProperty(string propertyName, string value)
        {
            var oxideProperty = typeof(Interface).GetProperty("Oxide", BindingFlags.Public | BindingFlags.Static);
            var oxide = oxideProperty.GetValue(null);
            
            var targetProperty = oxide.GetType().GetProperty(propertyName);
            targetProperty.SetValue(oxide, value);
        }

        [Fact]
        public void IsGlobal_ReturnsFalse()
        {
            Assert.False(langLib.IsGlobal);
        }

        [Fact]
        public void SetServerLanguage_UpdatesServerLanguage()
        {
            string newLang = "fr";
            langLib.SetServerLanguage(newLang);
            string serverLang = langLib.GetServerLanguage();
            Assert.Equal(newLang, serverLang);
        }

        [Fact]
        public void SetLanguage_UpdatesUserLanguage()
        {
            string userId = "user123";
            string userLang = "fr";
            
            // Set language for user
            langLib.SetLanguage(userLang, userId);
            
            // Get language for user
            string retrievedLang = langLib.GetLanguage(userId);
            Assert.Equal(userLang, retrievedLang);
        }

        [Fact]
        public void GetLanguage_DefaultsToServerLanguage()
        {
            // Set server language
            string serverLang = "es";
            langLib.SetServerLanguage(serverLang);
            
            // Get language for non-existent user
            string retrievedLang = langLib.GetLanguage("nonexistentuser");
            
            // Should return server language
            Assert.Equal(serverLang, retrievedLang);
        }

        [Fact]
        public void SetLanguage_NullOrEmptyParameters_DoesNothing()
        {
            // Set a known language for a user
            string userId = "user123";
            string userLang = "fr";
            langLib.SetLanguage(userLang, userId);
            
            // Try to set null or empty values
            langLib.SetLanguage(null, userId);
            langLib.SetLanguage("", userId);
            langLib.SetLanguage(userLang, null);
            langLib.SetLanguage(userLang, "");
            
            // Should still have the original language
            Assert.Equal(userLang, langLib.GetLanguage(userId));
        }

        [Fact]
        public void SetLanguage_SameValue_DoesNothing()
        {
            // Set a language for a user
            string userId = "user123";
            string userLang = "fr";
            langLib.SetLanguage(userLang, userId);
            
            // Set the same language again
            langLib.SetLanguage(userLang, userId);
            
            // Should still have the same language
            Assert.Equal(userLang, langLib.GetLanguage(userId));
        }

        [Fact]
        public void SetServerLanguage_SameValue_DoesNothing()
        {
            // Set a server language
            string serverLang = "fr";
            langLib.SetServerLanguage(serverLang);
            
            // Set the same language again
            langLib.SetServerLanguage(serverLang);
            
            // Should still have the same language
            Assert.Equal(serverLang, langLib.GetServerLanguage());
        }

        [Fact]
        public void SetServerLanguage_NullOrEmpty_DoesNothing()
        {
            // Set a known server language
            string serverLang = "fr";
            langLib.SetServerLanguage(serverLang);
            
            // Try to set null or empty values
            langLib.SetServerLanguage(null);
            langLib.SetServerLanguage("");
            
            // Should still have the original language
            Assert.Equal(serverLang, langLib.GetServerLanguage());
        }

        public void Dispose()
        {
            try
            {
                // Clean up temp directory
                if (Directory.Exists(testDataDir))
                    Directory.Delete(testDataDir, true);
            }
            catch (IOException)
            {
                // Ignore IO exceptions during cleanup
            }
        }
    }
} 