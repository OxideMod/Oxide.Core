using System;
using System.Collections.Generic;

namespace Oxide.DependencyInjection
{
    public class ServiceCollection : IServiceCollection
    {
        private readonly List<ServiceDescriptor> services;
        private volatile bool is_Modified;
        public bool IsModified => is_Modified;

        public ServiceCollection()
        {
            is_Modified = false;
            services = new List<ServiceDescriptor>();
        }

        public void Add(ServiceDescriptor item)
        {
            if (item == null)
            {
                return;
            }

            lock (services)
            {
                for (int i = 0; i < services.Count; i++)
                {
                    ServiceDescriptor s = services[i];
                    if (s.ServiceType != item.ServiceType)
                    {
                        continue;
                    }

                    services.Remove(s);
                    break;
                }

                services.Add(item);
                is_Modified = true;
            }
        }

        public IServiceProvider BuildServiceProvider()
        {
            lock (services)
            {
                return new ServiceProvider(new List<ServiceDescriptor>(services));
            }
        }

        internal IServiceProvider Internal_BuildServiceProvider()
        {
            is_Modified = false;
            return BuildServiceProvider();
        }
    }
}
