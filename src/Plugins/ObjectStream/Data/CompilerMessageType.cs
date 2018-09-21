using System;

namespace ObjectStream.Data
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
