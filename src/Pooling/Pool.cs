using System;
using System.Collections.Generic;
using System.Reflection;

namespace Oxide.Pooling
{
    public static class Pool
    {
        private static readonly Dictionary<Type, object> _claimables = new Dictionary<Type, object>();
        private static readonly MethodInfo _findMethod = typeof(Pool).GetMethod("Find", BindingFlags.NonPublic | BindingFlags.Static);

        public static T Claim<T>()
        {
            IClaimable<T> claim = Find<T>();
            return claim.Claim();
        }

        public static void Unclaim<T>(ref T item)
        {
            if (item == null)
            {
                return;
            }

            Type tType = typeof(T);
            Type instanceType = item.GetType();

            if (tType == instanceType)
            {
                IClaimable<T> claim = Find<T>();
                claim.Unclaim(item);
            }
            else
            {
                Type[] param = Pool.ClaimArray<Type>(1);
                param[0] = item.GetType();
                object claim = _findMethod.MakeGenericMethod(param).Invoke(null, ClaimArray<object>(0));
                Unclaim(ref param);
                MethodInfo method = claim.GetType().GetMethod("Unclaim", BindingFlags.Public | BindingFlags.Instance);
                object[] invoke = ClaimArray<object>(1);
                invoke[0] = item;
                method.Invoke(claim, invoke);
                Unclaim(ref invoke);
            }
            item = default;
        }

        public static List<T> ClaimList<T>() => Claim<List<T>>();

        public static T[] ClaimArray<T>(int length)
        {
            IArrayPool<T> claim = (IArrayPool<T>)Find<T[]>();
            return claim.Claim(length);
        }

        private static object Factory(Type itemType)
        {
            if (itemType.IsArray)
            {
                Type arrayType = typeof(ArrayPool<>).MakeGenericType(itemType.GetElementType());
                return Activator.CreateInstance(arrayType);
            }

            Type poolType = typeof(DynamicPool<>).MakeGenericType(itemType);
            return Activator.CreateInstance(poolType);
        }

        private static IClaimable<T> Find<T>()
        {
            Type elementType = typeof(T);

            lock (_claimables)
            {
                if (_claimables.TryGetValue(elementType, out object claim))
                {
                    return (IClaimable<T>)claim;
                }

                IClaimable<T> c = (IClaimable<T>)Factory(elementType);

                _claimables[elementType] = c;
                return c;
            }
        }
    }
}
