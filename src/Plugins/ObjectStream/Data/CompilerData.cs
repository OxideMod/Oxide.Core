using System;

namespace uMod.ObjectStream.Data
{
    [Serializable]
    class CompilerData
    {
        public bool LoadDefaultReferences { get; set; }
        public string AssemblyName { get; set; }
        public string OutputFile { get; set; }
        public CompilerPlatform Platform { get; set; }
        public CompilerFile[] ReferenceFiles { get; set; }
        public CompilerFile[] SourceFiles { get; set; }
        public bool StdLib { get; set; }
        public CompilerTarget Target { get; set; }
        public CompilerLanguageVersion Version { get; set; }

        public CompilerData()
        {
            Target = CompilerTarget.DynamicallyLinkedLibrary;
            Platform = CompilerPlatform.AnyCpu;
            Version = CompilerLanguageVersion.Default;
            LoadDefaultReferences = false;
            StdLib = false;
        }
    }
}
