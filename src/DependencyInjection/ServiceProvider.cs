using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Oxide.DependencyInjection
{
    internal class ServiceProvider : IServiceProvider
    {
        private static readonly Type ServiceType = typeof(IServiceProvider);
        private readonly ReadOnlyCollection<ServiceDescriptor> services;
        private readonly object[] serviceFactoryContext;

        internal ServiceProvider(List<ServiceDescriptor> services)
        {
            this.services =  services.AsReadOnly();
            serviceFactoryContext = new object[] { this };
        }

        public object GetService(Type serviceType)
        {
            if (ServiceType.IsAssignableFrom(serviceType))
            {
                return this;
            }

            ServiceDescriptor desc = null;
            using (IEnumerator<ServiceDescriptor> s = services.GetEnumerator())
            {
                while (s.MoveNext())
                {
                    if (s.Current == null || s.Current.ServiceType != serviceType)
                    {
                        continue;
                    }

                    desc = s.Current;
                    break;
                }
            }

            if (desc == null)
            {
                return null;
            }

            if (desc.Lifetime == ServiceLifetime.Transient)
            {
                if (desc.ImplementationInstance is Delegate @delegate)
                {
                    return @delegate.DynamicInvoke(serviceFactoryContext);
                }

                @delegate = ActivationUtility.CreateFactory(this, desc.ImplementationType);
                desc.SetInstance(@delegate);
                return @delegate.DynamicInvoke(serviceFactoryContext);
            }

            if (desc.ImplementationInstance == null)
            {
                desc.SetInstance(ActivationUtility.CreateInstance(this, desc.ImplementationType));
            }

            return desc.ImplementationInstance;
        }
    }
}
