using System.Collections.Generic;

namespace Oxide.Core.Plugins
{
    public class HookCache
    {
        private string NullKey = "null";

        public Dictionary<string, HookCache> _cache = new Dictionary<string, HookCache>();

        public List<HookMethod> _methods = null;

        public List<HookMethod> GetHookMethod(string hookName, object[] args, out HookCache cache)
        {
            //Interface.Oxide.ServerConsole.AddMessage($"GetHookMethod {hookName}");
            if (!_cache.TryGetValue(hookName, out HookCache nextCache))
            {
                nextCache = new HookCache();
                _cache.Add(hookName, nextCache);
            }
            return nextCache.GetHookMethod(args, 0, out cache);
        }

        public List<HookMethod> GetHookMethod(object[] args, int index, out HookCache cache)
        {
            if (args == null || index >= args.Length)
            {
                cache = this;
                return _methods;
            }

            HookCache nextCache;
            object arg = args[index];
            if (arg == null)
            {
                if (!_cache.TryGetValue(NullKey, out nextCache))
                {
                    nextCache = new HookCache();
                    _cache.Add(NullKey, nextCache);
                }
            }
            else
            {
                string fullName = arg.GetType().FullName;
                if (!_cache.TryGetValue(fullName, out nextCache))
                {
                    nextCache = new HookCache();
                    _cache.Add(fullName, nextCache);
                }
            }

            //Interface.Oxide.ServerConsole.AddMessage($"GetHookMethod {key} {index}");
            return nextCache.GetHookMethod(args, index + 1, out cache);
        }

        public void SetupMethods(List<HookMethod> methods)
        {
            _methods = methods;
        }
    }
}
