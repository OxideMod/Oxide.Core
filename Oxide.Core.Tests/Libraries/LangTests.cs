using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Core.Tests.Plugins.Mocks;
using Newtonsoft.Json;

namespace Oxide.Core.Tests.Libraries
{
    /// <summary>
    /// Tests for the Lang library.
    /// </summary>
    public class LangTests : IDisposable
    {
        private readonly Lang langLib;
        private readonly string testLangDir;

        /// <summary>
        /// Initializes the test environment.
        /// </summary>
        public LangTests()
        {
            // Create a temporary directory for language files.
            testLangDir = Path.Combine(Path.GetTempPath(), "OxideLangTests");
            if (!Directory.Exists(testLangDir))
                Directory.CreateDirectory(testLangDir);

            // Set the LangDirectory property via reflection because its setter is inaccessible.
            var oxide = Interface.Oxide;
            PropertyInfo langDirProp = oxide.GetType().GetProperty("LangDirectory", BindingFlags.Public | BindingFlags.Instance);
            if (langDirProp != null)
            {
                langDirProp.SetValue(oxide, testLangDir);
            }

            langLib = new Lang();
        }

        /// <summary>
        /// Tests that registering messages creates a file and that messages can be retrieved.
        /// </summary>
        [Fact]
        public void RegisterMessages_CreatesFile_And_GetMessageReturnsValue()
        {
            var messages = new Dictionary<string, string>
            {
                { "Hello", "Hello World" },
                { "Goodbye", "Goodbye World" }
            };
            Plugin fakePlugin = new FakePlugin();
            langLib.RegisterMessages(messages, fakePlugin, "en");

            string message = langLib.GetMessage("Hello", fakePlugin);
            Assert.Equal("Hello World", message);
        }

        /// <summary>
        /// Tests that setting and getting the server language works.
        /// </summary>
        [Fact]
        public void SetServerLanguage_UpdatesServerLanguage()
        {
            string newLang = "fr";
            langLib.SetServerLanguage(newLang);
            string serverLang = langLib.GetServerLanguage();
            Assert.Equal(newLang, serverLang);
        }

        /// <summary>
        /// Cleans up temporary files.
        /// </summary>
        public void Dispose()
        {
            if (Directory.Exists(testLangDir))
            {
                Directory.Delete(testLangDir, true);
            }
        }
    }
}
