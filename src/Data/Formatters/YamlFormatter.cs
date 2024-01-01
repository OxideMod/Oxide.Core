extern alias References;

using References::YamlDotNet.Serialization;
using System;
using System.IO;
using System.Text;
using Oxide.Data.Formatters.ContractResolvers;

namespace Oxide.Data.Formatters
{
    internal class YamlFormatter : IDataFormatter
    {
        public string FileExtension { get; }

        public string MimeType { get; }

        public Encoding Encoding { get; }

        protected ISerializer Serializer { get; }

        protected IDeserializer Deserializer { get; }

        public YamlFormatter(Encoding encoding = null, Serializer serializer = null, Deserializer deserializer = null)
        {
            Encoding = encoding ?? Encoding.UTF8;
            FileExtension = ".yaml";
            MimeType = $"application/x-yaml; charset={Encoding.WebName}";
            Serializer = serializer ?? new SerializerBuilder()
                .WithTypeInspector(inner => new YamlContractResolver(inner))
                .Build();
            Deserializer = deserializer ?? new DeserializerBuilder()
                .WithTypeInspector(inner => new YamlContractResolver(inner))
                .Build();
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
                object value = Deserializer.Deserialize(sr, graphType);

                if (existingGraph == null)
                {
                    return value;
                }

                XmlFormatter.CopyProperties(graphType, value, existingGraph);
                return existingGraph;
            }
        }
    }
}
