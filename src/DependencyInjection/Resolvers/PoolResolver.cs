using System;
using System.Reflection;
using Oxide.Pooling;

namespace Oxide.DependencyInjection.Resolvers
{
    internal sealed class PoolResolver : IDependencyResolver
    {
        private Type PoolType { get; }
        private Type ArrayPoolType { get; }

        private IPoolFactory Factory { get; }

        private MethodInfo GetMethod { get; }

        private IArrayPoolProvider<Type> TypePool { get; }
        private object[] EmptyArray { get; }

        public PoolResolver(IPoolFactory factory)
        {
            PoolType = typeof(IPoolProvider<>);
            ArrayPoolType = typeof(IArrayPoolProvider<>);
            Factory = factory;
            TypePool = factory.GetArrayProvider<Type>();
            EmptyArray = factory.GetArrayProvider<object>().Take(0);
            GetMethod = factory.GetType().GetMethod("GetProvider", BindingFlags.Public | BindingFlags.Instance);
        }

        public bool CanResolve(Type declaringType, Type requestedType, string name, DependencyResolveType resolveType, object obj)
        {
            if (!requestedType.IsGenericType)
            {
                return false;
            }

            Type generic = requestedType.GetGenericTypeDefinition();

            return PoolType == generic || ArrayPoolType == generic;
        }

        public object Resolve(Type declaringType, Type requestedType, string name, DependencyResolveType resolveType, object obj)
        {
            Type elementType = null;

            if (requestedType.GetGenericTypeDefinition() != PoolType)
            {
                Type[] interfaces = requestedType.GetInterfaces();
                for (int i = 0; i < interfaces.Length; i++)
                {
                    Type inter = interfaces[i];

                    if (!inter.IsGenericType || inter.GetGenericTypeDefinition() != PoolType)
                    {
                        continue;
                    }

                    elementType = inter.GetGenericArguments()[0];
                    break;
                }
            }
            else
            {
                elementType = requestedType.GetGenericArguments()[0];
            }

            if (elementType == null)
            {
                return null;
            }

            Type[] types = TypePool.Take(1);
            types[0] = elementType;
            MethodInfo method = GetMethod.MakeGenericMethod(types);
            TypePool.Return(types);
            return method.Invoke(Factory, EmptyArray);
        }
    }
}
