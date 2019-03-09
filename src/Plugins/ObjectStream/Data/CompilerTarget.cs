using System;

namespace uMod.ObjectStream.Data
{
    [Serializable]
    enum CompilerTarget
    {
        ConsoleApplication = 0,
        WindowsApplication = 1,
        DynamicallyLinkedLibrary = 2,
        NetModule = 3,
        WindowsRuntimeMetadata = 4,
        WindowsRuntimeApplication = 5
    }
}
