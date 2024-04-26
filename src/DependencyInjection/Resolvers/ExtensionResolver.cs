using System;
using System.Linq;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Pooling;

namespace Oxide.DependencyInjection.Resolvers
{
    internal class ExtensionResolver : IDependencyResolver
    {
        private static Type ExtensionType { get; } = typeof(Extension);
        private ExtensionManager Manager { get; }

        public ExtensionResolver(ExtensionManager ext)
        {
            Manager = ext;
        }

        public bool CanResolve(Type declaringType, Type requestedType, string name, DependencyResolveType resolveType, object obj)
        {
            return ExtensionType.IsAssignableFrom(requestedType);
        }

        public object Resolve(Type declaringType, Type requestedType, string name, DependencyResolveType resolveType, object obj)
        {
            return Manager.GetExtension(name) ?? Manager.GetExtension(requestedType.Name);
        }
    }
}
