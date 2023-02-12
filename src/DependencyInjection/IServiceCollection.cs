using System;

namespace Oxide.DependencyInjection
{
    public interface IServiceCollection
    {
        IServiceCollection AddService(ServiceDescriptor descriptor);
    }
}
