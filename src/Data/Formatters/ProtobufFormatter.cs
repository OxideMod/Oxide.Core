extern alias References;
using System;
using System.IO;
using Oxide.Core;
using References::ProtoBuf;
using References::ProtoBuf.Meta;

namespace Oxide.Data.Formatters
{
    internal class ProtobufFormatter : IDataFormatter
    {
        public string FileExtension { get; }

        public string MimeType { get; }

        private RuntimeTypeModel Settings { get; }

        public ProtobufFormatter(RuntimeTypeModel model = null)
        {
            FileExtension = ".data";
            MimeType = "application/protobuf";
            Settings = model;

            if (Settings != null) return;
            Settings = RuntimeTypeModel.Create(nameof(OxideMod));
            Settings.AutoAddMissingTypes = true;
            Settings.MetadataTimeoutMilliseconds = (int)Math.Abs(TimeSpan.FromSeconds(5).TotalMilliseconds);
            Settings.IncludeDateTimeKind = true;
            Settings.AllowParseableTypes = true;
            Settings.AutoCompile = true;
        }

        public void Serialize(Stream serializationStream, Type graphType, object graph)
        {
            EnsurePrepared(graphType);
            Settings.Serialize(serializationStream, graph);
        }

        public object Deserialize(Type graphType, Stream serializationStream, object existingGraph = null)
        {
            EnsurePrepared(graphType);
            return Settings.Deserialize(serializationStream, existingGraph, graphType);
        }

        protected virtual MetaType GetOrCreateModel(Type graph) => Settings.Add(graph, true);

        protected virtual void EnsurePrepared(Type type) => GetOrCreateModel(type);
    }
}
