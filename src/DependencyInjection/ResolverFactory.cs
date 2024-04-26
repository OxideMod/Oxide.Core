using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Logging;

namespace Oxide.DependencyInjection
{
    internal class ResolverFactory : IDependencyResolverFactory
    {
        private List<IDependencyResolver> ServiceResolvers { get; }
        private Logger Logger { get; }

        public ResolverFactory(Logger logger)
        {
            Logger = logger;
            ServiceResolvers = new List<IDependencyResolver>();
        }

        public object ResolveService(Type declaringType, Type requestedType, string name, DependencyResolveType resolveType, object obj)
        {
            lock (ServiceResolvers)
            {
                for (int i = 0; i < ServiceResolvers.Count; i++)
                {
                    IDependencyResolver resolver = ServiceResolvers[i];

                    if (!resolver.CanResolve(declaringType, requestedType, name, resolveType, obj))
                    {
                        continue;
                    }
#if DEBUG
                    Logger.Write(LogType.Debug, "Resolved {0} {1} for {2}", resolveType, requestedType, declaringType);
#endif
                    return resolver.Resolve(declaringType, requestedType, name, resolveType, obj);
                }
            }
#if DEBUG
            Logger.Write(LogType.Debug, "Failed to resolve {0} {1} for {2}", resolveType, requestedType, declaringType);
#endif
            return null;
        }

        public IDependencyResolverFactory RegisterServiceResolver<TResolver>() where TResolver : IDependencyResolver
        {
            Type type = typeof(TResolver);
            lock (ServiceResolvers)
            {
                for (int i = 0; i < ServiceResolvers.Count; i++)
                {
                    if (type != ServiceResolvers[i].GetType())
                    {
                        continue;
                    }
#if DEBUG
                    Logger.Write(LogType.Debug, "Resolver {0} already exists", type.FullName);
#endif
                    return this;
                }

                IDependencyResolver resolver = (IDependencyResolver)ActivationUtility.CreateInstance(Interface.Oxide?.ServiceProvider, type);
                ServiceResolvers.Add(resolver);
            }

#if DEBUG
            Logger.Write(LogType.Debug, "Resolver {0} added", type.FullName);
#endif
            return this;
        }
    }
}
