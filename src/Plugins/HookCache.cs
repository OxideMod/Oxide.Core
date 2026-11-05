#if !NET35
using System.Collections;
#endif
using System.Collections.Generic;

namespace Oxide.Core.Plugins
{
    public class HookCache
    {
        private static readonly object NullKey = new object();

#if NET35
        // Old Mono's Hashtable is not safe to read during a write, so net35 copies on write instead
        private volatile Dictionary<object, HookCache> _cache = new Dictionary<object, HookCache>();

        private readonly object _writeLock = new object();
#else
        private readonly Hashtable _cache = new Hashtable();
#endif

        private volatile List<HookMethod> _methods;

        public List<HookMethod> GetHookMethod(string hookName, object[] args, out HookCache cache)
        {
            return GetOrAdd(hookName).GetHookMethod(args, 0, out cache);
        }

        public List<HookMethod> GetHookMethod(object[] args, int index, out HookCache cache)
        {
            HookCache node = this;
            if (args != null)
            {
                for (int i = index; i < args.Length; i++)
                {
                    object arg = args[i];
                    node = node.GetOrAdd(arg == null ? NullKey : arg.GetType());
                }
            }

            cache = node;
            return node._methods;
        }

        public void SetupMethods(List<HookMethod> methods)
        {
            _methods = methods;
        }

#if NET35
        private HookCache GetOrAdd(object key)
        {
            if (_cache.TryGetValue(key, out HookCache next))
            {
                return next;
            }

            lock (_writeLock)
            {
                Dictionary<object, HookCache> current = _cache;
                if (current.TryGetValue(key, out next))
                {
                    return next;
                }

                next = new HookCache();
                _cache = new Dictionary<object, HookCache>(current) { { key, next } };
                return next;
            }
        }
#else
        private HookCache GetOrAdd(object key)
        {
            if (_cache[key] is HookCache next)
            {
                return next;
            }

            lock (_cache)
            {
                next = _cache[key] as HookCache;
                if (next == null)
                {
                    next = new HookCache();
                    _cache[key] = next;
                }

                return next;
            }
        }
#endif
    }
}
