using System;

namespace Oxide.Pooling
{
    public static class CorePoolingExtensions
    {
        public static IDisposable RegisterArrayPool<T>(this IPoolFactory factory, out IArrayPoolProvider<T> pool, int capacityPerLength = 150, int maxLength = 50)
        {
            if (factory.IsHandledType<T[]>())
            {
                throw new InvalidOperationException("Pool is already registered");
            }

            IDisposable dis = factory.RegisterProvider(out BaseArrayPoolProvider<T> p, capacityPerLength, maxLength);
            pool = p;
            return dis;
        }
    }
}
