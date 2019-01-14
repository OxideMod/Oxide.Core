using System.Collections.Generic;

namespace uMod.Plugins
{
    public class HookCache
    {
        private const string nullKey = "null";

        public Dictionary<string, HookCache> _cache = new Dictionary<string, HookCache>();

        public List<HookMethod> _methods;

        public List<HookMethod> GetHookMethod(string hookName, object[] args, out HookCache cache)
        {
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

            HookCache nextCache = null;
            if (args[index] == null)
            {
                if (!_cache.TryGetValue(nullKey, out nextCache))
                {
                    nextCache = new HookCache();
                    _cache.Add(nullKey, nextCache);
                }
            }
            else
            {
                string typeName = args[index].GetType().FullName;
                if (!_cache.TryGetValue(typeName, out nextCache))
                {
                    nextCache = new HookCache();
                    _cache.Add(typeName, nextCache);
                }
            }

            return nextCache.GetHookMethod(args, index + 1, out cache);
        }

        public void SetupMethods(List<HookMethod> methods)
        {
            _methods = methods;
        }
    }
}
