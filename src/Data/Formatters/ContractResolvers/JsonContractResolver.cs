extern alias References;
using System.Reflection;
using System.Runtime.Serialization;
using References::Newtonsoft.Json;
using References::Newtonsoft.Json.Serialization;

namespace Oxide.Data.Formatters.ContractResolvers
{
    internal class JsonContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (member.GetCustomAttribute<IgnoreDataMemberAttribute>(true) != null)
            {
                property.Ignored = true;
                return property;
            }

            DataMemberAttribute dataInfo = member.GetCustomAttribute<DataMemberAttribute>(true);

            if (dataInfo == null)
            {
                return property;
            }

            property.PropertyName = dataInfo.Name ?? property.PropertyName;
            property.Required = dataInfo.IsRequired ? Required.Always : Required.Default;

            if (!property.Order.HasValue && dataInfo.Order != default)
            {
                property.Order = dataInfo.Order;
            }

            property.DefaultValueHandling =
                dataInfo.EmitDefaultValue ? DefaultValueHandling.Include : DefaultValueHandling.Ignore;

            return property;
        }
    }
}
