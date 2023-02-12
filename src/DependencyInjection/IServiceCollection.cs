using System;

namespace Oxide.DependencyInjection
{
    public interface IServiceCollection
    {
        IServiceCollection AddService(Type serviceType, Type implementationType, object implementation, bool transient);
    }
}
