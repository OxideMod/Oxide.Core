namespace Oxide.Pooling
{
    public interface IPoolObject
    {
        bool ShouldPool { get; set; }

        void Cleanup();
    }
}
