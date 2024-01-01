extern alias References;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using References::YamlDotNet.Core;
using References::YamlDotNet.Serialization;
using References::YamlDotNet.Serialization.TypeInspectors;

namespace Oxide.Data.Formatters.ContractResolvers
{
    internal class YamlContractResolver : TypeInspectorSkeleton
    {
        private readonly ITypeInspector innerTypeDescriptor;

        public YamlContractResolver(ITypeInspector typeInspector)
        {
            innerTypeDescriptor = typeInspector ?? throw new ArgumentNullException(nameof(typeInspector));
        }

        public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container)
        {
            return innerTypeDescriptor.GetProperties(type, container)
                .Where(desc => !ShouldIgnore(desc))
                .Select(desc => (IPropertyDescriptor)new PropertyDescriptor(desc));
        }

        private bool ShouldIgnore(IPropertyDescriptor property)
        {
            return property.GetCustomAttribute<YamlIgnoreAttribute>() != null ||
                   property.GetCustomAttribute<IgnoreDataMemberAttribute>() != null;
        }

        private class PropertyDescriptor : IPropertyDescriptor
        {
            private readonly IPropertyDescriptor baseDescriptor;
            private DataMemberAttribute DataMember { get; }
            private DisplayNameAttribute DisplayName { get; }

            public PropertyDescriptor(IPropertyDescriptor baseDescriptor)
            {
                this.baseDescriptor = baseDescriptor;
                DataMember = baseDescriptor.GetCustomAttribute<DataMemberAttribute>();
                DisplayName = baseDescriptor.GetCustomAttribute<DisplayNameAttribute>();
            }

            public T GetCustomAttribute<T>() where T : Attribute => baseDescriptor.GetCustomAttribute<T>();

            public IObjectDescriptor Read(object target) => baseDescriptor.Read(target);

            public void Write(object target, object value) => baseDescriptor.Write(target, value);

            public string Name => GetName();

            public bool CanWrite => baseDescriptor.CanWrite;
            public Type Type => baseDescriptor.Type;

            public Type TypeOverride
            {
                get => baseDescriptor.TypeOverride;
                set => baseDescriptor.TypeOverride = value;
            }

            private string GetName()
            {
                if (DataMember != null && !string.IsNullOrEmpty(DataMember.Name))
                {
                    return DataMember.Name;
                }

                if (DisplayName != null && !string.IsNullOrEmpty(DisplayName.DisplayName))
                {
                    return DisplayName.DisplayName;
                }

                return baseDescriptor.Name;
            }

            public int Order
            {
                get => DataMember?.Order ?? baseDescriptor.Order;
                set
                {
                    if (DataMember != null)
                    {
                        DataMember.Order = value;
                    }

                    baseDescriptor.Order = value;
                }
            }

            public ScalarStyle ScalarStyle
            {
                get => baseDescriptor.ScalarStyle;
                set => baseDescriptor.ScalarStyle = value;
            }
        }
    }
}
