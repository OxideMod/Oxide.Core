using System;

namespace uMod.ObjectStream.Data
{
    [Serializable]
    enum CompilerPlatform
    {
        AnyCpu = 0,
        X86 = 1,
        X64 = 2,
        Itanium = 3,
        AnyCpu32BitPreferred = 4,
        Arm = 5,
        Arm64 = 6
    }
}
