using System;

namespace Oxide.DependencyInjection
{
    public class NoSuitableConstructorException : ArgumentException
    {
        /// <summary>
        /// The type with no suitable constructor
        /// </summary>
        public Type Type { get; }

        internal NoSuitableConstructorException(Type type, string paramName) : base($"No suitable constructor found for type ({type.FullName})", paramName)
        {
            Type = type;
        }
    }
}
