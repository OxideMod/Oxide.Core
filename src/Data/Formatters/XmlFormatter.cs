using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace Oxide.Data.Formatters
{
    internal class XmlFormatter : IDataFormatter
    {
        public string FileExtension { get; }

        public string MimeType { get; }

        protected Encoding Encoding { get; }

        protected XmlWriterSettings WriteSettings { get; }

        protected XmlReaderSettings ReadSettings { get; }

        public XmlFormatter(Encoding encoding, XmlWriterSettings writeSettings, XmlReaderSettings readerSettings)
        {
            FileExtension = ".xaml";
            MimeType = $"application/xaml+xml; charset={encoding.WebName}";
            Encoding = encoding;
            WriteSettings = writeSettings;
            ReadSettings = readerSettings;
        }

        public XmlFormatter() : this(Encoding.UTF8, new XmlWriterSettings() { Indent = true }, new XmlReaderSettings())
        {
        }

        public void Serialize(Stream serializationStream, Type graphType, object graph)
        {
            DataContractSerializer dc = new DataContractSerializer(graphType, graphType.Name, graphType.Namespace ?? string.Empty);
            using (StreamWriter sw = new StreamWriter(serializationStream, Encoding))
            using (XmlWriter xw = XmlWriter.Create(sw, WriteSettings))
            {
                dc.WriteObject(xw, graph);
            }
        }

        public object Deserialize(Type graphType, Stream serializationStream, object existingGraph = null)
        {
            DataContractSerializer dc = new DataContractSerializer(graphType, graphType.Name, graphType.Namespace ?? string.Empty);
            using (StreamReader sr = new StreamReader(serializationStream, Encoding))
            using (XmlReader xr = XmlReader.Create(sr, ReadSettings))
            {
                object data = dc.ReadObject(xr, true);

                if (existingGraph == null)
                {
                    return data;
                }

                // Workaround since Xml doesn't support merging
                CopyProperties(graphType, data, existingGraph);
                return existingGraph;
            }
        }

        internal static void CopyProperties(Type context, object source, object target)
        {
            if (source == null)
            {
                return;
            }

            PropertyInfo[] props = context.GetProperties(BindingFlags.Instance);

            for (int i = 0; i < props.Length; i++)
            {
                PropertyInfo c = props[i];

                if (!c.CanRead || !c.CanWrite || c.GetCustomAttribute<IgnoreDataMemberAttribute>(true) != null)
                {
                    continue;
                }

                object value = c.GetValue(target, null);

                if (value == null || c.PropertyType.IsPrimitive)
                {
                    c.SetValue(target, c.GetValue(source, null), null);
                    continue;
                }

                CopyProperties(c.PropertyType, c.GetValue(source, null), value);
            }
        }
    }
}
