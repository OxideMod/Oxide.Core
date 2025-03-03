using Oxide.Core.Plugins;
using System;

namespace Oxide.Core.Tests.Plugins.Mocks
{
    /// <summary>
    /// A fake plugin for testing purposes.
    /// </summary>
    public class FakePlugin : Plugin
    {
        public FakePlugin()
        {
            // Optionally set custom properties for testing.
            Name = "FakePlugin";
            Title = "Fake Plugin";
            Author = "Test";
            Version = new VersionNumber(1, 0, 0);
        }

        #region Methods
        public override void Load()
        {
            // Minimal load logic for testing.
        }

        protected override object OnCallHook(string hook, object[] args)
        {
            // Optionally raise an error event for testing.
            int argCount = args != null ? args.Length : 0;
            return $"Hook: {hook}, Args: {argCount}";
        }
        #endregion
    }
}
