namespace Oxide.Pooling
{
    public interface IPoolSource
    {
        void Free(ref object item);
    }

    public interface IPoolSource<T> : IPoolSource where T : class
    {
        T Get();
    }
}
