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
        public void RegisterMessages_WithNullParams_DoesNothing()
        {
            // Test all combinations of null parameters
            langLib.RegisterMessages(null, new TestPlugin(), "en");
            langLib.RegisterMessages(new Dictionary<string, string>(), null, "en");
            langLib.RegisterMessages(new Dictionary<string, string>(), new TestPlugin(), null);
            
            // No assertions needed - we're just verifying no exceptions are thrown
        }
        
        [Fact]
        public void RegisterMessages_CreatesLangFile()
        {
            // Arrange
            var plugin = new TestPlugin();
            var messages = new Dictionary<string, string>
            {
                { "greeting", "Hello" },
                { "farewell", "Goodbye" }
            };
            string lang = "en";
            Directory.CreateDirectory(Path.Combine(testLangDir, lang));
            
            // Act
            langLib.RegisterMessages(messages, plugin, lang);
            
            // Assert
            string filePath = Path.Combine(testLangDir, lang, $"{plugin.Name}.json");
            Assert.True(File.Exists(filePath));
        }
        
        [Fact]
        public void GetMessage_ReturnsMessageForKey()
        {
            // Arrange
            var plugin = new TestPlugin();
            var messages = new Dictionary<string, string>
            {
                { "greeting", "Hello" },
                { "farewell", "Goodbye" }
            };
            string lang = "en";
            Directory.CreateDirectory(Path.Combine(testLangDir, lang));
            langLib.RegisterMessages(messages, plugin, lang);
            
            // Act
            string message = langLib.GetMessage("greeting", plugin);
            
            // Assert
            Assert.Equal("Hello", message);
        }
        
        [Fact]
        public void GetMessage_WithMissingKey_ReturnsKeyAsMessage()
        {
            // Arrange
            var plugin = new TestPlugin();
            var messages = new Dictionary<string, string>
            {
                { "greeting", "Hello" }
            };
            string lang = "en";
            Directory.CreateDirectory(Path.Combine(testLangDir, lang));
            langLib.RegisterMessages(messages, plugin, lang);
            
            // Act
            string message = langLib.GetMessage("unknown_key", plugin);
            
            // Assert
            Assert.Equal("unknown_key", message);
        }
        
        [Fact]
        public void GetMessage_WithNullKey_ReturnsKey()
        {
            // Arrange
            var plugin = new TestPlugin();
            
            // Act
            string message = langLib.GetMessage(null, plugin);
            
            // Assert
            Assert.Null(message);
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
        public void GetMessages_ReturnsAllMessagesForPlugin()
        {
            // Arrange
            var plugin = new TestPlugin();
            var messages = new Dictionary<string, string>
            {
                { "greeting", "Hello" },
                { "farewell", "Goodbye" }
            };
            string lang = "en";
            Directory.CreateDirectory(Path.Combine(testLangDir, lang));
            langLib.RegisterMessages(messages, plugin, lang);
            
            // Act
            var result = langLib.GetMessages(lang, plugin);
            
            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Hello", result["greeting"]);
            Assert.Equal("Goodbye", result["farewell"]);
        }
        
        [Fact]
        public void GetMessages_WithInvalidParams_ReturnsNull()
        {
            // Arrange
            var plugin = new TestPlugin();
            
            // Act & Assert
            Assert.Null(langLib.GetMessages(null, plugin));
            Assert.Null(langLib.GetMessages("en", null));
            Assert.Null(langLib.GetMessages("", plugin));
        }
        
        [Fact]
        public void GetMessageByLanguage_ReturnsMessageInSpecificLanguage()
        {
            // Arrange
            var plugin = new TestPlugin();
            
            // Register English messages
            var enMessages = new Dictionary<string, string> { { "greeting", "Hello" } };
            Directory.CreateDirectory(Path.Combine(testLangDir, "en"));
            langLib.RegisterMessages(enMessages, plugin, "en");
            
            // Register French messages
            var frMessages = new Dictionary<string, string> { { "greeting", "Bonjour" } };
            Directory.CreateDirectory(Path.Combine(testLangDir, "fr"));
            langLib.RegisterMessages(frMessages, plugin, "fr");
            
            // Act
            string enMessage = langLib.GetMessageByLanguage("greeting", plugin, "en");
            string frMessage = langLib.GetMessageByLanguage("greeting", plugin, "fr");
            
            // Assert
            Assert.Equal("Hello", enMessage);
            Assert.Equal("Bonjour", frMessage);
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
        public void GetLanguages_WithPlugin_ReturnsLanguagesForPlugin()
        {
            // Arrange
            var plugin = new TestPlugin();
            Directory.CreateDirectory(Path.Combine(testLangDir, "en"));
            Directory.CreateDirectory(Path.Combine(testLangDir, "fr"));
            Directory.CreateDirectory(Path.Combine(testLangDir, "de"));
            
            // Create language files for the plugin
            File.WriteAllText(Path.Combine(testLangDir, "en", $"{plugin.Name}.json"), "{}");
            File.WriteAllText(Path.Combine(testLangDir, "fr", $"{plugin.Name}.json"), "{}");
            
            // Act
            string[] languages = langLib.GetLanguages(plugin);
            
            // Assert
            Assert.Equal(2, languages.Length);
            Assert.Contains("en", languages);
            Assert.Contains("fr", languages);
            Assert.DoesNotContain("de", languages);
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
    
    /// <summary>
    /// A simple plugin for testing purposes
    /// </summary>
    public class TestPlugin : Plugin
    {
        public TestPlugin()
        {
            Name = "TestPlugin";
            Title = "Test Plugin";
            Author = "Test Author";
            Version = new VersionNumber(1, 0, 0);
        }
        
        protected override object OnCallHook(string hook, object[] args)
        {
            return null;
        }
    }
} 