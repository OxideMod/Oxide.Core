using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Oxide.Core.Tests
{
    [CollectionDefinition("Oxide Sequential Tests", DisableParallelization = true)]
    public class OxideSequentialTestCollection { }
}
