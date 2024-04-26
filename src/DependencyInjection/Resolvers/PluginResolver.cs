using System;
using Oxide.Core.Plugins;

namespace Oxide.DependencyInjection.Resolvers
{
    internal class PluginResolver : IDependencyResolver
    {
        private static Type PluginType { get; } = typeof(Plugin);
        private PluginManager Plugins { get; }

        public PluginResolver(PluginManager manager)
        {
            Plugins = manager;
        }

        public bool CanResolve(Type declaringType, Type requestedType, string name, DependencyResolveType resolveType, object obj)
        {
            return PluginType.IsAssignableFrom(requestedType);
        }

        public object Resolve(Type declaringType, Type requestedType, string name, DependencyResolveType resolveType, object obj)
        {
            return Plugins.GetPlugin(name) ?? Plugins.GetPlugin(requestedType.Name);
        }
    }
}
