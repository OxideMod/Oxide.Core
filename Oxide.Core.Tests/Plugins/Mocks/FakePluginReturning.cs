using Oxide.Core.Plugins;

namespace Oxide.Core.Tests.Plugins.Mocks
{
    /// <summary>
    /// A plugin that always returns the specified value from OnCallHook.
    /// </summary>
    public class FakePluginReturning : Plugin
    {
        private readonly string _returnValue;

        public FakePluginReturning(string returnValue)
        {
            _returnValue = returnValue;
            Name = "FakePluginReturning";
        }

        #region Methods
        public override void Load()
        {
            // No custom logic
        }

        protected override object OnCallHook(string hook, object[] args)
        {
            // Always return the same value
            return _returnValue;
        }
        #endregion
    }
}
