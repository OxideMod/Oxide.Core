using System;

namespace Oxide.DependencyInjection
{
    internal sealed class ServiceDescriptor
    {
        public bool IsTransient { get; }

        public Type ServiceType { get; }

        public Type ImplementationType { get; }

        public object Implementation { get; internal set; }

        internal ServiceDescriptor(Type serviceType, Type implementationType, object implementation, bool isTransient)
        {
            IsTransient = isTransient;
            ServiceType = serviceType;
            ImplementationType = implementationType;
            Implementation = implementation;
        }
    }
}
