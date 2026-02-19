using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Oxide.Pooling;

namespace Oxide.Core.Plugins
{
    public class HookMethod
    {
        public string Name;

        public MethodInfo Method;

        public ParameterInfo[] Parameters { get; set; }

        public bool IsBaseHook { get; set; }

        public HookMethod(MethodInfo method)
        {
            Method = method;
            Name = method.Name;

            Parameters = Method.GetParameters();

            int parameterCount = Parameters.Length;
            if (parameterCount > 0)
            {
                List<string> parameterNames = PoolFactory<List<string>>.Shared.Take();
                try
                {
                    for (int i = 0; i < parameterCount; i++)
                    {
                        ParameterInfo parameter = Parameters[i];
                        string name = parameter.ParameterType.ToString();
                        parameterNames.Add(name);
                    }

                    Name = $"{Name}({parameterNames.JoinValues(", ")})";
                }
                finally
                {
                    parameterNames.Clear();
                    PoolFactory<List<string>>.Shared.Return(parameterNames);
                }
            }

            IsBaseHook = Name.StartsWith("base_");
        }

        public bool HasMatchingSignature(object[] args, out bool exact)
        {
            exact = true;

            if (Parameters.Length == 0 && (args == null || args.Length == 0))
            {
                return true;
            }

            int argCount = args.Length;
            for (int i = 0; i < argCount; i++)
            {
                Type parameterType = Parameters[i].ParameterType;

                if (args[i] == null)
                {
                    if (CanAssignNull(parameterType))
                    {
                        continue;
                    }

                    return false;
                }

                Type argType = args[i].GetType();
                if (exact)
                {
                    if (argType != parameterType && argType.MakeByRefType() != parameterType &&
                        !CanConvertNumber(args[i], parameterType))
                    {
                        exact = false;
                    }
                }

                if (exact)
                {
                    continue;
                }

                if (argType == parameterType || argType.MakeByRefType() == parameterType ||
                    parameterType.FullName == "System.Object")
                {
                    continue;
                }

                if (argType.IsValueType)
                {
                    if (!TypeDescriptor.GetConverter(parameterType).CanConvertFrom(argType) &&
                        !CanConvertNumber(args[i], parameterType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!parameterType.IsInstanceOfType(args[i]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool CanAssignNull(Type type)
        {
            if (!type.IsValueType)
            {
                return true;
            }

            return Nullable.GetUnderlyingType(type) != null;
        }

        private bool IsNumber(object? obj)
        {
            if (obj == null)
            {
                return false;
            }

            Type objectType = obj.GetType();
            return IsNumber(Nullable.GetUnderlyingType(objectType) ?? objectType);
        }

        private bool IsNumber(Type type)
        {
            if (type.IsPrimitive)
            {
                return type != typeof(bool) && type != typeof(char) && type != typeof(IntPtr) && type != typeof(UIntPtr);
            }

            return type == typeof(decimal);
        }

        private bool CanConvertNumber(object value, Type type)
        {
            if (!IsNumber(value) || !IsNumber(type))
            {
                return false;
            }

            return TypeDescriptor.GetConverter(type).IsValid(value);
        }
    }
}
