using System;
using System.Collections.Generic;
using System.IO;
using Oxide.Core;
using Oxide.Core.Libraries;

namespace Oxide.IO
{
    internal abstract class CachedFileSystemWatcher : BaseFileSystemWatcher
    {
        private class Cache
        {
            public string Directory;

            public string Name;

            public NotifyMask Mask;

            public Timer.TimerInstance Timer;
        }

        private readonly Queue<Cache> cachePool;
        private readonly Dictionary<string, Cache> callbackCache;
        private readonly Timer timer;

        protected CachedFileSystemWatcher(string directory, bool subDirs, NotifyMask filter, IFileSystem fs, StringComparison comparer) : base(directory, subDirs, filter, fs, comparer)
        {
            cachePool = new Queue<Cache>();
            callbackCache = new Dictionary<string, Cache>();
            timer = Interface.Oxide.GetLibrary<Timer>();
        }

        protected override void OnFileSystemEvent(string directory, string name, NotifyMask mask)
        {
            if ((mask & NotifyMask.DirectoryOnly) == NotifyMask.DirectoryOnly)
            {
                base.OnFileSystemEvent(directory, name, mask);
                return;
            }

            if (ShouldIgnore(directory, name, mask))
            {
                return;
            }

            string fullPath = Path.Combine(directory, name);
            switch (mask)
            {
                default:
                    base.OnFileSystemEvent(directory, name, mask);
                    return;

                case NotifyMask.OnDeleted:
                    Clear(fullPath);
                    base.OnFileSystemEvent(directory, name, mask);
                    return;

                case NotifyMask.OnCreated:
                case NotifyMask.OnModified:
                    break;
            }

            if (callbackCache.TryGetValue(fullPath, out Cache cache))
            {
                cache.Timer.DestroyToPool();
                cache.Timer = timer.Once(0.5f, () =>
                {
                    OnTimerFired(fullPath, cache);
                });

                return;
            }

            cache = GetCache();
            cache.Directory = directory;
            cache.Name = name;
            cache.Mask = mask;
            callbackCache[fullPath] = cache;
            LogDebug($"[BeginCache] Dir: {directory} | Name: {name} | Mask: {mask}");
            cache.Timer = timer.Once(0.5f, () =>
            {
                OnTimerFired(fullPath, cache);
            });
        }

        private void Clear(string path)
        {
            if (!callbackCache.TryGetValue(path, out Cache cache))
            {
                return;
            }

            callbackCache.Remove(path);
            ReturnCache(cache);
        }

        private void OnTimerFired(string path, Cache cache)
        {
            LogDebug($"[{nameof(OnTimerFired)}] Dir: {cache.Directory} | Name: {cache.Name} | Mask: {cache.Mask}");
            callbackCache.Remove(path);
            base.OnFileSystemEvent(cache.Directory, cache.Name, cache.Mask);
            ReturnCache(cache);
        }

        private Cache GetCache()
        {
            if (cachePool.Count == 0)
            {
                return new Cache();
            }

            return cachePool.Dequeue();
        }

        private void ReturnCache(Cache cache)
        {
            if (cachePool.Count >= 50)
            {
                return;
            }

            cache.Directory = null;
            cache.Name = null;
            cache.Mask = default;
            cache.Timer.DestroyToPool();
            cache.Timer = null;
            cachePool.Enqueue(cache);
        }
    }
}
