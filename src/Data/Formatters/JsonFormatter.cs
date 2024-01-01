extern alias References;

using References::Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Oxide.Data.Formatters.ContractResolvers;

namespace Oxide.Data.Formatters
{
    internal class JsonFormatter : IDataFormatter
    {
        public string FileExtension { get; }

        public string MimeType { get; }

        public Encoding Encoding { get; }

        public JsonSerializer Serializer { get; }

        public JsonFormatter(Encoding encoding = null, JsonSerializerSettings settings = null)
        {
            Encoding = encoding ?? Encoding.UTF8;
            FileExtension = ".json";
            MimeType = $"application/json; charset={Encoding.WebName}";
            if (settings == null)
            {
                settings = new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                    DateParseHandling = DateParseHandling.DateTimeOffset,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    FloatParseHandling = FloatParseHandling.Double,
                    FloatFormatHandling = FloatFormatHandling.DefaultValue,
                    DefaultValueHandling = DefaultValueHandling.Populate,
                    MetadataPropertyHandling = MetadataPropertyHandling.Default,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
                    ObjectCreationHandling = ObjectCreationHandling.Reuse,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    ContractResolver = new JsonContractResolver(),
                    Culture = CultureInfo.InvariantCulture
                };
            }
            Serializer = JsonSerializer.Create(settings);
        }

        public void Serialize(Stream serializationStream, Type graphType, object graph)
        {
            using (StreamWriter sw = new StreamWriter(serializationStream, Encoding))
            {
                Serializer.Serialize(sw, graph, graphType);
            }
        }

        public object Deserialize(Type graphType, Stream serializationStream, object existingGraph = null)
        {
            using (StreamReader sr = new StreamReader(serializationStream, Encoding))
            {
                if (existingGraph != null)
                {
                    Serializer.Populate(sr, existingGraph);
                    return existingGraph;
                }

                return Serializer.Deserialize(sr, graphType);
            }
        }
    }
}
