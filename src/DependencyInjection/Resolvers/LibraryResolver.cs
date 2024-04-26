using System;
using Oxide.Core.Extensions;
using Oxide.Core.Libraries;

namespace Oxide.DependencyInjection.Resolvers
{
    internal class LibraryResolver : IDependencyResolver
    {
        private static Type LibraryType { get; } = typeof(Library);
        private ExtensionManager ExtensionManager { get; }
        private IServiceProvider Provider { get; }

        public LibraryResolver(ExtensionManager ext, IServiceProvider provider)
        {
            ExtensionManager = ext;
            Provider = provider;
        }

        public bool CanResolve(Type declaringType, Type requestedType, string name, DependencyResolveType resolveType, object obj)
        {
            return LibraryType.IsAssignableFrom(requestedType);
        }

        public object Resolve(Type declaringType, Type requestedType, string name, DependencyResolveType resolveType, object obj)
        {
            return requestedType.IsAbstract
                #pragma warning disable CS0618
                ? ExtensionManager.GetLibrary(name)
                #pragma warning restore CS0618
                : Provider.GetService(requestedType);
        }
    }
}
