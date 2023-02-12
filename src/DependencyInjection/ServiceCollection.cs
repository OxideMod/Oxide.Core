using System;
using System.Collections.Generic;

namespace Oxide.DependencyInjection
{
    public class ServiceCollection : IServiceProvider, IServiceCollection
    {
        private readonly Type _serviceType = typeof(IServiceProvider);
        private readonly object[] _callContext;
        private readonly Dictionary<Type, ServiceDescriptor> descriptors = new Dictionary<Type, ServiceDescriptor>();

        public ServiceCollection()
        {
            _callContext = new object[] { this };
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == null)
            {
                return null;
            }

            if (_serviceType == serviceType)
            {
                return this;
            }

            ServiceDescriptor descriptor = null;
            lock (descriptors)
            {
                if (!descriptors.TryGetValue(serviceType, out descriptor))
                {
                    return null;
                }
            }

            lock (descriptor)
            {
                if (descriptor.IsTransient)
                {
                    if (descriptor.Implementation is Delegate d)
                    {
                        return d.DynamicInvoke(_callContext);
                    }

                    Func<IServiceProvider, object> factory = ActivationUtility.CreateFactory(this, descriptor.ImplementationType);
                    descriptor.Implementation = factory;

                    return factory?.Invoke(this);
                }

                if (descriptor.Implementation != null)
                {
                    return descriptor.Implementation;
                }
                descriptor.Implementation = ActivationUtility.CreateInstance(this, descriptor.ImplementationType, null);
                return descriptor.Implementation;
            }
        }

        private void AddService(ServiceDescriptor descriptor)
        {
            lock (descriptor)
            {
                descriptors.Add(descriptor.ServiceType, descriptor);
            }
        }

        public IServiceCollection AddService(Type serviceType, Type implementationType, object implementation, bool transient)
        {
            AddService(new ServiceDescriptor(serviceType, implementationType, implementation, transient));
            return this;
        }
    }
}
