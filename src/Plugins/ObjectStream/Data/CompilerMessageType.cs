using System;

namespace uMod.ObjectStream.Data
{
    [Serializable]
    enum CompilerMessageType
    {
        Assembly,
        Compile,
        Error,
        Exit,
        Ready
    }
}
