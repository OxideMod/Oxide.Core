using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Oxide.Pooling
{
    internal class CorePoolFactory : IPoolFactory
    {
        private readonly Type arrayType = typeof(IArrayPoolProvider<>);
        private readonly Type itemType = typeof(IPoolProvider<>);
        private readonly Dictionary<Type, IPoolProvider> registeredProviders;

        public CorePoolFactory()
        {
            registeredProviders = new Dictionary<Type, IPoolProvider>
            {
                // [typeof(object[])] = new BaseArrayPoolProvider<object>(),
                // [typeof(StringBuilder)] = new StringPoolProvider()
            };
        }

        public IPoolProvider<T> GetProvider<T>()
        {
            lock (registeredProviders)
            {
                return registeredProviders.TryGetValue(typeof(T), out IPoolProvider provider) ? (IPoolProvider<T>)provider : null;
            }
        }

        public bool IsHandledType<T>()
        {
            lock (registeredProviders)
            {
                return registeredProviders.ContainsKey(typeof(T));
            }
        }

        public IDisposable RegisterProvider<T>(out T provider, params object[] args) where T : IPoolProvider
        {
            provider = default;
            Type providerType = typeof(T);
            Type genericType = null;
            Type[] interfaces = providerType.GetInterfaces();

            for (int i = 0; i < interfaces.Length; i++)
            {
                Type c = interfaces[i];
                bool isArrayType = false;

                if (!c.IsGenericType)
                {
                    continue;
                }

                Type baseInterface = c.GetGenericTypeDefinition();
                Debug.Print($"Input Interface: {c.FullName}\n" +
                            $"Base Interface: {baseInterface.FullName}");

                if (arrayType == baseInterface)
                {
                    isArrayType = true;
                    Debug.Print($"Type is {arrayType.Name}");
                }
                else if (itemType == baseInterface)
                {
                    Debug.Print($"Type is {itemType.Name}");
                }
                else
                {
                    Debug.Print($"{c.Name} is not a supported interface of this factory");
                    continue;
                }

                genericType = isArrayType ? c.GetGenericArguments()[0].MakeArrayType() : c.GetGenericArguments()[0];
                break;
            }

            if (genericType == null)
            {
                throw new ArgumentNullException(nameof(genericType));
            }

            Debug.Print($"GenericType is {genericType.FullName}");

            lock (registeredProviders)
            {
                if (!registeredProviders.ContainsKey(genericType))
                {
                    provider = (T)Activator.CreateInstance(providerType, args);
                    registeredProviders[genericType] = provider;
                    Debug.Print($"Registered pooling provider for {genericType.FullName}");
                    return new ProviderExpirationToken(genericType, this);
                }
                else
                {
                    throw new ArgumentException($"A provider already exists for key {genericType.FullName}");
                }
            }
        }

        private class ProviderExpirationToken : IDisposable
        {
            private readonly Type key;
            private readonly CorePoolFactory instance;

            public ProviderExpirationToken(Type key, CorePoolFactory instance)
            {
                this.key = key;
                this.instance = instance;
            }

            ~ProviderExpirationToken() => Dispose(false);

            public void Dispose() => Dispose(true);

            private void Dispose(bool disposing)
            {
                if (disposing)
                {
                    GC.SuppressFinalize(this);
                }

                lock (instance.registeredProviders)
                {
                    if (instance.registeredProviders.Remove(key))
                    {
                        Debug.Print($"Unregistered PoolProvider {key.FullName}");
                    }
                }
            }
        }
    }
}
