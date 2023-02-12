using Oxide.Pooling;
using System;
using System.Reflection;

namespace Oxide.DependencyInjection
{
    public static class ActivationUtility
    {
        internal class ParameterMap : IDisposable, IResetable
        {
            public MemberInfo Member { get; private set; }

            public ParameterInfo[] Parameters { get; private set; }

            public object[] Arguments { get; private set; }

            public int?[] PositionMap { get; private set; }

            public int Mapped { get; private set; }

            public int Found { get; private set; }

            public bool Init(MemberInfo member)
            {
                Member = member;
                if (member is ConstructorInfo constructor)
                {
                    Parameters = constructor.GetParameters();
                }
                else if (member is MethodInfo method)
                {
                    Parameters = method.GetParameters();
                }
                else
                {
                    return false;
                }

                Arguments = Pool.ClaimArray<object>(Parameters.Length);
                PositionMap = Pool.ClaimArray<int?>(Parameters.Length);

                return true;
            }

            public void Dispose() => Reset();

            public void Reset()
            {
                Member = null;
                Parameters = null;
                object[] args = Arguments;
                Arguments = null;
                Pool.Unclaim(ref args);
                int?[] map = PositionMap;
                PositionMap = null;
                Pool.Unclaim(ref map);
                Mapped = 0;
                Found = 0;
            }

            public bool Map(IServiceProvider provider, object[] parameters)
            {
                for (int i = 0; i < Parameters.Length; i++)
                {
                    ParameterInfo param = Parameters[i];
                    if (PositionMap[param.Position].HasValue)
                    {
                        continue;
                    }

                    Type paramType = param.ParameterType;

                    if (parameters == null || parameters.Length == 0)
                    {
                        object val = provider?.GetService(paramType);

                        if (val == null)
                        {
                            return false;
                        }

                        Arguments[param.Position] = val;
                        Found++;
                    }
                    else
                    {
                        for (int a = 0; a < parameters.Length; a++)
                        {
                            if (IsMapped(a))
                            {
                                continue;
                            }

                            object value = parameters[a];

                            if (value == null)
                            {
                                if (param.IsOut || paramType.IsByRef)
                                {
                                    SetMappedPosition(param, value, a);
                                }
                                else
                                {
                                    value = provider?.GetService(paramType);

                                    if (value == null)
                                    {
                                        return false;
                                    }

                                    Arguments[param.Position] = value;
                                    Found++;
                                }
                                break;
                            }

                            if (!paramType.IsInstanceOfType(value))
                            {
                                value = provider?.GetService(paramType);

                                if (value == null)
                                {
                                    return false;
                                }

                                Arguments[param.Position] = value;
                                Found++;
                                break;
                            }

                            SetMappedPosition(param, value, a);
                            break;
                        }
                    }
                }

                return true;
            }

            private void SetMappedPosition(ParameterInfo parameter, object value, int callbackIndex)
            {
                Arguments[parameter.Position] = value;
                PositionMap[parameter.Position] = callbackIndex;
                Mapped++;
                Found++;
            }

            private bool IsMapped(int position)
            {
                for (int i = 0; i < PositionMap.Length; i++)
                {
                    int? c = PositionMap[i];
                    if (c.HasValue && c.Value == position)
                    {
                        return true;
                    }
                }

                return false;
            }

            public void ProcessOuts(object[] parameters)
            {
                if (parameters == null || parameters.Length == 0)
                {
                    return;
                }

                for (int i = 0; i < PositionMap.Length; i++)
                {
                    int? c = PositionMap[i];

                    if (!c.HasValue)
                    {
                        continue;
                    }

                    ParameterInfo p = Parameters[i];

                    if (p.IsOut || p.ParameterType.IsByRef)
                    {
                        parameters[c.Value] = Arguments[p.Position];
                    }
                }
            }
        }

        internal static ConstructorInfo FindSuitableConstructor(ConstructorInfo[] constructors, object[] parameters, IServiceProvider provider, out ParameterMap map)
        {
            map = null;
            ConstructorInfo constructor = null;

            for (int i = 0; i < constructors.Length; i++)
            {
                ConstructorInfo con = constructors[i];
                ParameterMap currMap = Pool.Claim<ParameterMap>();
                currMap.Init(con);
                if (!currMap.Map(provider, parameters))
                {
                    continue;
                }

                if (map != null)
                {
                    if (currMap.Mapped > map.Mapped)
                    {
                        Pool.Unclaim(ref map);
                        map = currMap;
                        constructor = con;
                        continue;
                    }
                    else if (currMap.Mapped == map.Mapped && currMap.Found > map.Found)
                    {
                        Pool.Unclaim(ref map);
                        map = currMap;
                        constructor = con;
                        continue;
                    }
                    else
                    {
                        Pool.Unclaim(ref currMap);
                        continue;
                    }
                }
                map = currMap;
                constructor = con;
            }

            return constructor;
        }

        public static object CreateInstance(IServiceProvider serviceProvider, Type type, object[] additionalParams)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (additionalParams == null)
            {
                additionalParams = Pool.ClaimArray<object>(0);
            }

            if (serviceProvider == null)
            {
                return Activator.CreateInstance(type, additionalParams);
            }

            ConstructorInfo constructor = FindSuitableConstructor(type.GetConstructors(), additionalParams, serviceProvider, out ParameterMap map);
            if (constructor == null)
            {
                throw new NoSuitableConstructorException(type, nameof(type));
            }

            try
            {
                object instance = constructor.Invoke(map.Arguments);
                return instance;
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException != null)
                {
                    throw e.InnerException;
                }

                throw;
            }
            finally
            {
                Pool.Unclaim(ref map);
            }
        }

        internal static object CreateInstance(ConstructorInfo constructor, IServiceProvider provider, object[] parameters)
        {
            if (parameters == null)
            {
                parameters = Pool.ClaimArray<object>(0);
            }

            ParameterMap map = Pool.Claim<ParameterMap>();
            map.Init(constructor);
            map.Map(provider, parameters);
            object instance = constructor.Invoke(map.Arguments);
            Pool.Unclaim(ref map);
            return instance;
        }

        public static Func<IServiceProvider, object> CreateFactory(IServiceProvider serviceProvider, Type type)
        {
            ConstructorInfo constructor = FindSuitableConstructor(type.GetConstructors(BindingFlags.Public), null, serviceProvider, out ParameterMap map);

            if (constructor == null)
            {
                throw new NoSuitableConstructorException(type, nameof(type));
            }

            Func<IServiceProvider, object> factory = (s) => CreateInstance(constructor, s, null);
            Pool.Unclaim(ref map);
            return factory;
        }
    }
}
