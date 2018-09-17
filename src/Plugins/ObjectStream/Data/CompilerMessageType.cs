using System;

namespace uMod.Plugins.ObjectStream.Data
{
    [Serializable]
    internal enum CompilerMessageType
    {
        Assembly,
        Compile,
        Error,
        Exit,
        Ready
    }
}
