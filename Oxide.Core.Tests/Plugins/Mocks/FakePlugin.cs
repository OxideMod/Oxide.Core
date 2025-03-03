using Oxide.Core.Plugins;
using System;

namespace Oxide.Core.Tests.Plugins.Mocks
{
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

        public override void Load()
        {
            // Minimal load logic for testing.
        }

        protected override object OnCallHook(string hook, object[] args)
        {
            // For testing purposes, simply return a string combining the hook name and the number of arguments.
            int argCount = args != null ? args.Length : 0;
            return $"Hook: {hook}, Args: {argCount}";
        }
    }
}
