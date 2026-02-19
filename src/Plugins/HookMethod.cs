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

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == null)
                {
                    if (CanAssignNull(Parameters[i].ParameterType))
                    {
                        continue;
                    }

                    return false;
                }

                if (exact)
                {
                    if (args[i].GetType() != Parameters[i].ParameterType &&
                        args[i].GetType().MakeByRefType() != Parameters[i].ParameterType &&
                        !CanConvertNumber(args[i], Parameters[i].ParameterType))
                    {
                        exact = false;
                    }
                }

                if (exact)
                {
                    continue;
                }

                if (args[i].GetType() == Parameters[i].ParameterType ||
                    args[i].GetType().MakeByRefType() == Parameters[i].ParameterType ||
                    Parameters[i].ParameterType.FullName == "System.Object")
                {
                    continue;
                }

                if (args[i].GetType().IsValueType)
                {
                    if (!TypeDescriptor.GetConverter(Parameters[i].ParameterType).CanConvertFrom(args[i].GetType()) && !CanConvertNumber(args[i], Parameters[i].ParameterType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Parameters[i].ParameterType.IsInstanceOfType(args[i]))
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

        private bool IsNumber(object obj)
        {
            return obj != null && IsNumber(Nullable.GetUnderlyingType(obj.GetType()) ?? obj.GetType());
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
