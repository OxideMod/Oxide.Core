extern alias References;

using System;
using System.Globalization;

using References::Newtonsoft.Json;

namespace Oxide.Core
{
    public class VersionNumberShortConverter : JsonConverter
    {
        // Cache some constant values
        private static readonly char[] separators = new char[1] { '.' };
        private static readonly Type vNumberType = typeof(VersionNumber);

        /// <summary>
        /// Serialize object if it is a <see cref="VersionNumber"/>, throw an exception if it is not
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Type vType = value?.GetType();

            if (vType != vNumberType)
            {
                throw new JsonSerializationException("Expected value of type VersionNumber, but got " + (vType?.Name ?? "null"));
            }

            writer.WriteValue(ConvertToString((VersionNumber)value));
        }

        /// <summary>
        /// Try deserialize json value as <see cref="VersionNumber"/>, throw an exception if failed
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                try
                {
                    return ParseFromString((string)reader.Value);
                }
                catch (Exception e)
                {
                    throw GenerateException(reader, "Failed to parse VersionNumber from '{0}': {1}", reader.Value, e.Message);
                }
            }

            throw GenerateException(
                reader,
                "Unexpected token '{0}' ({1}) on VersionNumber deserialization. Expected: 'String'",
                reader.TokenType,
                reader.Value ?? "null"
            );
        }

        /// <summary>
        /// Check if serialized object is <see cref="VersionNumber"/>
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == vNumberType;
        }

        // Helper to generate JSE with additional info about line and col of the error token
        private static JsonSerializationException GenerateException(JsonReader reader, string format, params object[] args)
        {
            string message = string.Format(format, args);
            IJsonLineInfo lineInfo = (IJsonLineInfo)reader;

            if (lineInfo.HasLineInfo())
            {
                message += $" at {lineInfo.LineNumber}:{lineInfo.LinePosition}";
            }

            return new JsonSerializationException(message);
        }

        // Just a wrapper for consistency
        private string ConvertToString(VersionNumber number)
        {
            return number.ToString();
        }

        // Parse string which matches x|x.x|x.x.x format
        private VersionNumber ParseFromString(string strNumber)
        {
            string[] array = strNumber.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            if (array.Length < 1 || array.Length > 3)
            {
                throw new ArgumentException(
                    "String does not match the VersionNumber serialization format",
                    nameof(strNumber)
                );
            }

            int[] iArray = new int[3];

            for (int i = 0; i < array.Length; i++)
            {
                string s = array[i];

                int v = int.Parse(s, NumberStyles.Integer);

                iArray[i] = v;
            }

            return new VersionNumber(iArray[0], iArray[1], iArray[2]);
        }
    }
}
