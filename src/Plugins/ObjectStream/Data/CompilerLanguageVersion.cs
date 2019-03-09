using System;

namespace uMod.ObjectStream.Data
{
    [Serializable]
    public enum CompilerLanguageVersion
    {
        Default = 0,
        CSharp1 = 1,
        CSharp2 = 2,
        CSharp3 = 3,
        CSharp4 = 4,
        CSharp5 = 5,
        CSharp6 = 6,
        CSharp7 = 7,
        CSharp7_1 = 701,
        CSharp7_2 = 702,
        CSharp7_3 = 703,
        Latest = int.MaxValue
    }
}
