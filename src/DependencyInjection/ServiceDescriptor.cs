using System;

namespace Oxide.DependencyInjection
{
    public sealed class ServiceDescriptor
    {
        public bool IsTransient { get; }

        public Type ServiceType { get; }

        public Type ImplementationType { get; }

        public object Implementation { get; internal set; }

        private ServiceDescriptor(Type serviceType, Type implementationType, object implementation, bool isTransient)
        {
            IsTransient = isTransient;
            ServiceType = serviceType;
            ImplementationType = implementationType;
            Implementation = implementation;
        }

        public static ServiceDescriptor CreateTransient(Type serviceType, Type implementationType, Delegate factory = null)
        {
            if (implementationType == null)
            {
                throw new ArgumentNullException(nameof(implementationType));
            }

            if (implementationType.IsAbstract || implementationType.IsInterface || implementationType.IsValueType)
            {
                throw new ArgumentException("Type must not be abstract and not a value type", nameof(implementationType));
            }

            if (implementationType != null && serviceType == null)
            {
                serviceType = implementationType;
            }

            if (!serviceType.IsAssignableFrom(implementationType))
            {
                throw new ArgumentException($"ServiceType: {serviceType.FullName} <= ImplementationType: {implementationType.FullName} not assignable", nameof(serviceType));
            }

            if (factory != null)
            {
                Type type = typeof(Func<,>);
                type = type.MakeGenericType(typeof(IServiceProvider), implementationType);

                if (!type.IsInstanceOfType(factory))
                {
                    throw new ArgumentException("Func does not return implementation type", nameof(factory));
                }
            }

            return new ServiceDescriptor(serviceType, implementationType, factory, true);
        }

        public static ServiceDescriptor CreateSingleton(Type serviceType, Type implementationType, object implementation = null)
        {
            if (implementation != null)
            {
                implementationType = implementation.GetType();
            }

            if (implementationType == null)
            {
                throw new ArgumentNullException(nameof(implementationType));
            }

            if (implementationType.IsAbstract || implementationType.IsInterface || implementationType.IsValueType)
            {
                throw new ArgumentException("Type must not be abstract and not a value type", nameof(implementationType));
            }

            if (implementationType != null && serviceType == null)
            {
                serviceType = implementationType;
            }

            if (!serviceType.IsAssignableFrom(implementationType))
            {
                throw new ArgumentException($"ServiceType: {serviceType.FullName} <= ImplementationType: {implementationType.FullName} not assignable", nameof(serviceType));
            }

            if (implementation != null && !serviceType.IsInstanceOfType(implementation))
            {
                throw new ArgumentException($"ServiceType: {serviceType.FullName} <= Implementation: {implementation.GetType().FullName} not assignable", nameof(serviceType));
            }

            return new ServiceDescriptor(serviceType, implementationType, implementation, false);
        }

        private static bool TryGetFuncType(Delegate @delegate, out Type returnType)
        {
            Type func = typeof(Func<,>);

            if (!func.IsInstanceOfType(@delegate))
            {
                returnType = null;
                return false;
            }

            func = @delegate.GetType();

            Type[] types = func.GetGenericArguments();
            returnType = types[1];
            return true;
        }
    }
}
