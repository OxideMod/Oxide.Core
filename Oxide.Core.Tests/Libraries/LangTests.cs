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
            // Arrange
            string userId = "user123";
            string language = "es";
            
            // Act
            langLib.SetLanguage(language, userId);
            string userLang = langLib.GetLanguage(userId);
            
            // Assert
            Assert.Equal(language, userLang);
        }
        
        [Fact]
        public void GetLanguage_WithUnknownUser_ReturnsServerLanguage()
        {
            // Arrange
            string unknownUserId = "unknown_user";
            string serverLang = "en";
            langLib.SetServerLanguage(serverLang);
            
            // Act
            string result = langLib.GetLanguage(unknownUserId);
            
            // Assert
            Assert.Equal(serverLang, result);
        }
        
        [Fact]
        public void GetLanguage_WithEmptyUserId_ReturnsServerLanguage()
        {
            // Arrange
            string emptyUserId = "";
            string serverLang = "en";
            langLib.SetServerLanguage(serverLang);
            
            // Act
            string result = langLib.GetLanguage(emptyUserId);
            
            // Assert
            Assert.Equal(serverLang, result);
        }
        
        [Fact]
        public void SetLanguage_WithNullOrEmptyLang_DoesNothing()
        {
            // Arrange
            string userId = "user123";
            string initialLang = "fr";
            langLib.SetLanguage(initialLang, userId);
            
            // Act
            langLib.SetLanguage(null, userId);
            langLib.SetLanguage("", userId);
            
            // Assert
            Assert.Equal(initialLang, langLib.GetLanguage(userId));
        }
        
        [Fact]
        public void SetLanguage_WithNullOrEmptyUserId_DoesNothing()
        {
            // Arrange
            string language = "de";
            
            // Act & Assert - Should not throw exceptions
            langLib.SetLanguage(language, null);
            langLib.SetLanguage(language, "");
        }
        
        [Fact]
        public void SetServerLanguage_WithNullOrEmptyLang_DoesNothing()
        {
            // Arrange
            string initialLang = "fr";
            langLib.SetServerLanguage(initialLang);
            
            // Act
            langLib.SetServerLanguage(null);
            langLib.SetServerLanguage("");
            
            // Assert
            Assert.Equal(initialLang, langLib.GetServerLanguage());
        }
        
        [Fact]
        public void GetMessage_WithNullPlugin_ReturnsKey()
        {
            // Act
            string message = langLib.GetMessage("greeting", null);
            
            // Assert
            Assert.Equal("greeting", message);
        }
        
        [Fact]
        public void GetLanguages_ReturnsAllAvailableLanguages()
        {
            // Arrange
            Directory.CreateDirectory(Path.Combine(testLangDir, "en"));
            Directory.CreateDirectory(Path.Combine(testLangDir, "fr"));
            Directory.CreateDirectory(Path.Combine(testLangDir, "de"));
            
            // Create some files to make the directories non-empty
            File.WriteAllText(Path.Combine(testLangDir, "en", "test.json"), "{}");
            File.WriteAllText(Path.Combine(testLangDir, "fr", "test.json"), "{}");
            File.WriteAllText(Path.Combine(testLangDir, "de", "test.json"), "{}");
            
            // Act
            string[] languages = langLib.GetLanguages();
            
            // Assert
            Assert.Equal(3, languages.Length);
            Assert.Contains("en", languages);
            Assert.Contains("fr", languages);
            Assert.Contains("de", languages);
        }
        
        [Fact]
        public void LoadFromDatafile_LoadsData()
        {
            // Arrange
            string userId = "user123";
            string language = "es";
            
            // Act - Set user language and create a new Lang instance to load data
            langLib.SetLanguage(language, userId);
            var newLangLib = new Lang();
            
            // Assert - Verify data was loaded
            Assert.Equal(language, newLangLib.GetLanguage(userId));
        }
        
        [Fact]
        public void SaveData_SavesAllData()
        {
            // Arrange
            string userId1 = "user123";
            string userId2 = "user456";
            string lang1 = "es";
            string lang2 = "fr";
            string serverLang = "de";
            
            // Act
            langLib.SetLanguage(lang1, userId1);
            langLib.SetLanguage(lang2, userId2);
            langLib.SetServerLanguage(serverLang);
            
            // Create a new instance to ensure data is loaded from storage
            var newLangLib = new Lang();
            
            // Assert
            Assert.Equal(lang1, newLangLib.GetLanguage(userId1));
            Assert.Equal(lang2, newLangLib.GetLanguage(userId2));
            Assert.Equal(serverLang, newLangLib.GetServerLanguage());
        }

        public void Dispose()
        {
            // Clean up test directories
            try
            {
                if (Directory.Exists(testDataDir))
                {
                    Directory.Delete(testDataDir, true);
                }
            }
            catch (IOException)
            {
                // Ignore errors during cleanup
            }
        }
    }
} 